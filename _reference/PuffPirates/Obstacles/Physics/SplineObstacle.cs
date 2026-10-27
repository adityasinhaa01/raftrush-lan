using PuffPirates.Boats;
using UnityEngine;
using UnityEngine.Splines;

namespace PuffPirates.Obstacles.Physics
{
	[RequireComponent(typeof(Rigidbody))]
	public sealed class SplineObstacle : PhysicsObstacleBase
	{
		public enum EndBehaviorMode
		{
			OneWay = 0,
			Loop = 1,
			PingPong = 2,
			TeleportToStart = 3
		}

		public enum BoatEffectMode
		{
			Push = 0,
			Drag = 1
		}

		[Header("Spline (Sahne Nesnesi)")]
		[Tooltip("Yolu tanımlayan SplineContainer — tasarımcının SAHNEDE çizdiği spline. Prefab bunu içinde SAKLAYAMAZ (bir prefab sahne nesnesine referans veremez); her yerleştirmede Inspector'dan elle atanmalıdır. Atanmamışsa engel bir kez uyarı loglar ve idle kalır.")]
		[SerializeField]
		private SplineContainer _spline;

		[Header("Aktif Segment (spline yüzdesi, 0..1)")]
		[Tooltip("Hareketin BAŞLADIĞI spline parametresi. Örn. 0.2 = spline'ın %20'si.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _startPercent;

		[Tooltip("Hareketin BİTTİĞİ spline parametresi. startPercent'ten KÜÇÜK de olabilir — bu durumda engel spline üzerinde TERS yönde hareket eder (ekstra kod gerekmez, yön işareti otomatik türer).")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _endPercent = 1f;

		[Header("Hareket Profili")]
		[Tooltip("Tepe hız (birim/sn).")]
		[SerializeField]
		[Min(0.01f)]
		private float _speed = 4f;

		[Tooltip("İvme (birim/sn²) — durgun konumdan tepe hıza çıkış süresini belirler.")]
		[SerializeField]
		[Min(0.01f)]
		private float _acceleration = 6f;

		[Tooltip("Yavaşlama (birim/sn²) — tepe hızdan uç noktada durmaya kadar geçen süreyi belirler. (Loop modu bunu KASITLI olarak YOK SAYAR — bkz. Uç Davranışı.)")]
		[SerializeField]
		[Min(0.01f)]
		private float _deceleration = 6f;

		[Header("Yön Hizalama")]
		[Tooltip("Açık: gövde spline teğetine (o anki hareket yönüne) döner.")]
		[SerializeField]
		private bool _rotateToPath = true;

		[Tooltip("Dönüş yumuşaklığı — saniyede yakınsama oranı. Yüksek = hedef yöne hızla kilitlenir (az yumuşama). Düşük = yavaşça döner. 0 = asla dönmez (Awake anındaki yönde donar kalır).")]
		[SerializeField]
		[Min(0f)]
		private float _turnSmoothness = 8f;

		[Header("Uç Davranışı")]
		[Tooltip("OneWay: durur. Loop: kesintisiz sarar. PingPong: gidip gelir. TeleportToStart: durup başa ışınlanır.")]
		[SerializeField]
		private EndBehaviorMode _endBehavior = EndBehaviorMode.PingPong;

		[Header("Çoklu Örnek (aynı spline üzerinde)")]
		[Tooltip("Zaman ekseninde faz kaydırma (sn) — aynı spline üzerinde birden çok örneği birbirinden ARALIKLI tutmak için (Piston/RotorArm'daki _phase alanıyla AYNI desen). Her örnek FARKLI bir değer alırsa aynı spline'da eşit aralıklı birden fazla engel çalıştırılabilir.")]
		[SerializeField]
		private float _phaseOffset;

		[Header("Bot Etkileşimi (Host-only)")]
		[Tooltip("Açık: temas eden bota HOST-ONLY ek kuvvet uygulanır (Push/Drag). Kapalı (varsayılan): engel yalnız kinematik PhysX çarpışmasıyla botu iter, ekstra kuvvet uygulanmaz.")]
		[SerializeField]
		private bool _affectBoats;

		[Tooltip("Push: hareket yönünde itki. Drag: botu platform hızına doğru çekip 'taşır'.")]
		[SerializeField]
		private BoatEffectMode _boatEffectMode;

		[Tooltip("Push modunda itki büyüklüğü; Drag modunda botun hızını platform hızına çekme oranı (1/sn).")]
		[SerializeField]
		[Min(0f)]
		private float _boatForce = 10f;

		private Rigidbody _rb;

		private bool _hasValidSpline;

		private bool _warnedInvalidSpline;

		private float _segmentLength;

		private Quaternion _currentRot;

		private Vector3 _lastValidDir;

		private Vector3 _currentMoveDirection;

		private float _currentSpeed;

		public Vector3 CurrentMoveDirection => _currentMoveDirection;

		public float CurrentSpeed => _currentSpeed;

		protected override void Awake()
		{
			base.Awake();
			_rb = GetComponent<Rigidbody>();
			if (_rb != null)
			{
				_rb.isKinematic = true;
				_rb.useGravity = false;
				_rb.interpolation = RigidbodyInterpolation.Interpolate;
				_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			}
			_currentRot = base.transform.rotation;
			_lastValidDir = base.transform.forward;
			_currentMoveDirection = base.transform.forward;
			ValidateAndCacheSpline();
		}

		private void ValidateAndCacheSpline()
		{
			_hasValidSpline = false;
			if (_spline == null)
			{
				WarnInvalidOnce("[SplineObstacle] '" + base.name + "': _spline atanmamış — engel idle kalacak.");
				return;
			}
			Spline spline = _spline.Spline;
			if (spline == null || spline.Count < 2)
			{
				WarnInvalidOnce("[SplineObstacle] '" + base.name + "': spline en az 2 knot içermeli — engel idle kalacak.");
				return;
			}
			if (Mathf.Approximately(_startPercent, _endPercent))
			{
				WarnInvalidOnce("[SplineObstacle] '" + base.name + "': startPercent == endPercent — hareket mesafesi sıfır, engel idle kalacak.");
				return;
			}
			_segmentLength = EstimateArcLength(_startPercent, _endPercent);
			if (_segmentLength <= 0.0001f)
			{
				WarnInvalidOnce("[SplineObstacle] '" + base.name + "': aktif segmentin yay-uzunluğu ~0 — engel idle kalacak.");
			}
			else
			{
				_hasValidSpline = true;
			}
		}

		private void WarnInvalidOnce(string message)
		{
			if (!_warnedInvalidSpline)
			{
				_warnedInvalidSpline = true;
				Debug.LogWarning(message, this);
			}
		}

		private float EstimateArcLength(float fromT, float toT)
		{
			Vector3 a = _spline.EvaluatePosition(Mathf.Clamp01(fromT));
			float num = 0f;
			for (int i = 1; i <= 48; i++)
			{
				float value = fromT + (toT - fromT) * ((float)i / 48f);
				Vector3 vector = _spline.EvaluatePosition(Mathf.Clamp01(value));
				num += Vector3.Distance(a, vector);
				a = vector;
			}
			return num;
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			if (!_hasValidSpline || _rb == null)
			{
				return;
			}
			float localTime = time + _phaseOffset;
			ComputeMotion(localTime, out var distance, out var speed, out var directionSign);
			float t = Mathf.Clamp01(distance / _segmentLength);
			float t2 = Mathf.Clamp01(Mathf.Lerp(_startPercent, _endPercent, t));
			Vector3 position = _spline.EvaluatePosition(t2);
			_rb.MovePosition(position);
			Vector3 vector = _spline.EvaluateTangent(t2);
			Vector3 vector2 = ((vector.sqrMagnitude > 0.0001f) ? (vector.normalized * directionSign) : _lastValidDir);
			if (vector2.sqrMagnitude > 0.0001f)
			{
				_lastValidDir = vector2;
			}
			_currentMoveDirection = vector2;
			_currentSpeed = speed;
			if (_rotateToPath)
			{
				if (vector2.sqrMagnitude > 0.0001f)
				{
					Vector3 upwards = ((Mathf.Abs(Vector3.Dot(vector2.normalized, Vector3.up)) > 0.999f) ? Vector3.forward : Vector3.up);
					Quaternion b = Quaternion.LookRotation(vector2, upwards);
					float t3 = Mathf.Clamp01(_turnSmoothness * dt);
					_currentRot = Quaternion.Slerp(_currentRot, b, t3);
				}
				_rb.MoveRotation(_currentRot);
			}
		}

		private void ComputeMotion(float localTime, out float distance, out float speed, out float directionSign)
		{
			float num = Mathf.Sign(_endPercent - _startPercent);
			switch (_endBehavior)
			{
			case EndBehaviorMode.OneWay:
				distance = EvaluateLeg(localTime, out speed);
				directionSign = num;
				break;
			case EndBehaviorMode.TeleportToStart:
			{
				float num6 = ComputeLegDuration();
				float t2 = ((num6 > 0f) ? Mathf.Repeat(localTime, num6) : 0f);
				distance = EvaluateLeg(t2, out speed);
				directionSign = num;
				break;
			}
			case EndBehaviorMode.PingPong:
			{
				float num7 = ComputeLegDuration();
				float num8 = num7 * 2f;
				float num9 = ((num8 > 0f) ? Mathf.Repeat(localTime, num8) : 0f);
				if (num9 <= num7)
				{
					distance = EvaluateLeg(num9, out speed);
					directionSign = num;
					break;
				}
				float t3 = num9 - num7;
				float num10 = EvaluateLeg(t3, out speed);
				distance = _segmentLength - num10;
				directionSign = 0f - num;
				break;
			}
			default:
			{
				float num2 = Mathf.Max(0.01f, _acceleration);
				float num3 = Mathf.Max(0.01f, _speed);
				float num4 = num3 / num2;
				float num5 = 0.5f * num2 * num4 * num4;
				float t;
				if (localTime < num4)
				{
					t = 0.5f * num2 * localTime * localTime;
					speed = num2 * Mathf.Max(0f, localTime);
				}
				else
				{
					t = num5 + num3 * (localTime - num4);
					speed = num3;
				}
				distance = Mathf.Repeat(t, _segmentLength);
				directionSign = num;
				break;
			}
			}
		}

		private float ComputeLegDuration()
		{
			ComputeLegProfile(out var _, out var _, out var _, out var _, out var _, out var _, out var legDuration, out var _);
			return legDuration;
		}

		private void ComputeLegProfile(out float ta, out float da, out float tc, out float dc, out float td, out float dd, out float legDuration, out float peakSpeed)
		{
			float num = Mathf.Max(0.01f, _acceleration);
			float num2 = Mathf.Max(0.01f, _deceleration);
			float num3 = Mathf.Max(0.01f, _speed);
			float num4 = Mathf.Max(0f, _segmentLength);
			float num5 = num3 / num;
			float num6 = 0.5f * num * num5 * num5;
			float num7 = num3 / num2;
			float num8 = 0.5f * num2 * num7 * num7;
			if (num6 + num8 >= num4)
			{
				peakSpeed = Mathf.Sqrt(2f * num4 * num * num2 / (num + num2));
				ta = peakSpeed / num;
				da = 0.5f * num * ta * ta;
				td = peakSpeed / num2;
				dd = num4 - da;
				tc = 0f;
				dc = 0f;
			}
			else
			{
				peakSpeed = num3;
				ta = num5;
				da = num6;
				td = num7;
				dd = num8;
				dc = num4 - da - dd;
				tc = dc / num3;
			}
			legDuration = ta + tc + td;
		}

		private float EvaluateLeg(float t, out float speedOut)
		{
			ComputeLegProfile(out var ta, out var da, out var tc, out var dc, out var _, out var _, out var legDuration, out var peakSpeed);
			if (legDuration <= 0f || _segmentLength <= 0f)
			{
				speedOut = 0f;
				return 0f;
			}
			if (t <= 0f)
			{
				speedOut = 0f;
				return 0f;
			}
			if (t >= legDuration)
			{
				speedOut = 0f;
				return _segmentLength;
			}
			float num = Mathf.Max(0.01f, _acceleration);
			float num2 = Mathf.Max(0.01f, _deceleration);
			if (t < ta)
			{
				speedOut = num * t;
				return 0.5f * num * t * t;
			}
			if (t < ta + tc)
			{
				speedOut = peakSpeed;
				return da + peakSpeed * (t - ta);
			}
			float num3 = t - ta - tc;
			speedOut = Mathf.Max(0f, peakSpeed - num2 * num3);
			return da + dc + peakSpeed * num3 - 0.5f * num2 * num3 * num3;
		}

		private void OnCollisionStay(Collision collision)
		{
			if (!_affectBoats || collision == null)
			{
				return;
			}
			NetworkBoat componentInParent = collision.collider.GetComponentInParent<NetworkBoat>();
			if (componentInParent == null || !componentInParent.IsServerInitialized)
			{
				return;
			}
			Rigidbody rigidbody = componentInParent.Rigidbody;
			if (!(rigidbody == null))
			{
				float num = (componentInParent.IsSpawned ? ((float)componentInParent.TimeManager.TickDelta) : Time.fixedDeltaTime);
				switch (_boatEffectMode)
				{
				case BoatEffectMode.Push:
					rigidbody.AddForce(_currentMoveDirection * (_boatForce * num), ForceMode.Impulse);
					break;
				case BoatEffectMode.Drag:
				{
					Vector3 b = _currentMoveDirection * _currentSpeed;
					rigidbody.linearVelocity = Vector3.Lerp(rigidbody.linearVelocity, b, Mathf.Clamp01(_boatForce * num));
					break;
				}
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (_spline == null)
			{
				return;
			}
			Spline spline = _spline.Spline;
			if (spline != null && spline.Count >= 1)
			{
				Vector3 c = _spline.EvaluatePosition(0f);
				Vector3 c2 = _spline.EvaluatePosition(1f);
				PhysicsObstacleBase.GizmoSphere(c, 0.35f, new Color(0.2f, 1f, 0.4f, 0.9f));
				PhysicsObstacleBase.GizmoSphere(c2, 0.35f, new Color(1f, 0.3f, 0.3f, 0.9f));
				Vector3 vector = _spline.EvaluatePosition(Mathf.Clamp01(_startPercent));
				Color color = Gizmos.color;
				Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
				for (int i = 1; i <= 24; i++)
				{
					float value = _startPercent + (_endPercent - _startPercent) * ((float)i / 24f);
					Vector3 vector2 = _spline.EvaluatePosition(Mathf.Clamp01(value));
					Gizmos.DrawLine(vector, vector2);
					vector = vector2;
				}
				Gizmos.color = color;
				Vector3 vector3 = (Application.isPlaying ? base.transform.position : ((Vector3)_spline.EvaluatePosition(Mathf.Clamp01(_startPercent))));
				Vector3 vector4;
				if (Application.isPlaying && _currentMoveDirection.sqrMagnitude > 0.0001f)
				{
					vector4 = _currentMoveDirection;
				}
				else
				{
					Vector3 vector5 = _spline.EvaluateTangent(Mathf.Clamp01(_startPercent));
					vector4 = ((vector5.sqrMagnitude > 0.0001f) ? (vector5.normalized * Mathf.Sign(_endPercent - _startPercent)) : Vector3.zero);
				}
				if (vector4.sqrMagnitude > 0.0001f)
				{
					PhysicsObstacleBase.GizmoArrow(vector3, vector4.normalized * 2f, new Color(0.2f, 0.6f, 1f, 0.95f));
				}
				if (_affectBoats && vector4.sqrMagnitude > 0.0001f)
				{
					float num = 1f + Mathf.Clamp(_boatForce * 0.05f, 0f, 3f);
					PhysicsObstacleBase.GizmoArrow(vector3 + Vector3.up * 0.5f, vector4.normalized * num, new Color(1f, 0.5f, 0f, 0.95f));
				}
			}
		}
	}
}
