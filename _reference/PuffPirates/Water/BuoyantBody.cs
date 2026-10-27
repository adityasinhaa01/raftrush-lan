using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Water
{
	[DisallowMultipleComponent]
	public sealed class BuoyantBody : MonoBehaviour
	{
		[SerializeField]
		private Rigidbody _rb;

		[Header("Yoğunluk (Arşimet)")]
		[Tooltip("Obje yoğunluğu (kg/m³). WaterChannel su yoğunluğundan KÜÇÜKSE yüzer, BÜYÜKSE batar. Hacim = rigidbody kütlesi / bu yoğunluk → OYUN KÜTLESİ korunur. Botlar ~450, su 1000.")]
		[SerializeField]
		[Min(1f)]
		private float _bodyDensity = 450f;

		[Tooltip("Kuvvete tam katkı için gereken batık derinlik (m). Üstünde doygunluk.")]
		[SerializeField]
		[Min(0.05f)]
		private float _maxDepth = 1f;

		[Header("Su sürtünmesi")]
		[SerializeField]
		[Min(0f)]
		private float _linearDrag = 1.2f;

		[SerializeField]
		[Range(0f, 1f)]
		private float _angularDrag = 0.35f;

		[Tooltip("Hızlı suya girişte DALIŞ freni: batıkken AŞAĞI hızı, en derin batış oranıyla ölçekli güçle söndürür → yüksek hızda derine dalıp batmayı önler. 0 = kapalı. Normal yüzmede dikey hız küçük olduğundan etkisi ihmal edilir.")]
		[SerializeField]
		[Min(0f)]
		private float _diveBrake = 8f;

		[Header("Denge — dik durma (hızda ters dönmeyi engeller)")]
		[Tooltip("Dik-tutma yay torku: bot yan/ters yatınca dünya-yukarısına döndürür. 0 = kapalı. YAW (dönüş) serbest kalır.")]
		[SerializeField]
		[Min(0f)]
		private float _uprightTorque = 20f;

		[Tooltip("Dik-tutma sönümü: pitch/roll salınımını keser (çok düşük = zıplar).")]
		[SerializeField]
		[Min(0f)]
		private float _uprightDamping = 3f;

		[Tooltip("Kütle merkezi Y (yerel). Buoyancy noktalarının ALTINA → bot BOTTOM-HEAVY, kendiliğinden dik durur (hızda/çarpmada devrilmez). Botlar için ~-0.7. Gemi omurgası (keel) etkisi.")]
		[SerializeField]
		private float _centerOfMassY = -0.7f;

		[Header("Yumuşatma / limitler (kafayı yeme + beklenmedik hareket önleme)")]
		[Tooltip("Su yüzeyi normalinin dikeyden MAX eğimi (derece). Dik/gürültülü yerlerde aşırı yanal kuvveti keser.")]
		[SerializeField]
		[Range(0f, 60f)]
		private float _maxSurfaceTilt = 20f;

		[Tooltip("Normal yumuşatma (0-1). Düşük = daha yumuşak/az jitter.")]
		[SerializeField]
		[Range(0.02f, 1f)]
		private float _normalSmooth = 0.1f;

		[Tooltip("Maks açısal hız (rad/s). Bot fırıl fırıl dönüp kafayı yemesin. 0 = limit yok.")]
		[SerializeField]
		[Min(0f)]
		private float _maxAngularSpeed = 2.5f;

		[Header("Örnek noktalar (yerel, hull hacmini temsil eder)")]
		[SerializeField]
		private Vector3[] _points = new Vector3[5]
		{
			new Vector3(-0.75f, -0.15f, -0.75f),
			new Vector3(0.75f, -0.15f, -0.75f),
			new Vector3(-0.75f, -0.15f, 0.75f),
			new Vector3(0.75f, -0.15f, 0.75f),
			new Vector3(0f, -0.15f, 0f)
		};

		private WaterChannel _channel;

		private readonly Dictionary<WaterChannel, int> _channelRefs = new Dictionary<WaterChannel, int>();

		private readonly List<WaterChannel> _channelOrder = new List<WaterChannel>();

		private bool _comInit;

		private Vector3 _smoothNormal = Vector3.up;

		private const float DebugArrowScale = 0.15f;

		private WaterChannel _sampled;

		private int _sampledFrame = -1;

		public bool DrivenExternally { get; set; }

		public float DisplacementMass { get; set; }

		private WaterChannel Current
		{
			get
			{
				if (_channel != null)
				{
					return _channel;
				}
				if (_rb == null || !_rb.isKinematic)
				{
					return null;
				}
				if (_sampledFrame != Time.frameCount)
				{
					_sampledFrame = Time.frameCount;
					WaterSystem.TrySampleWater(base.transform.position, out var _, out _sampled);
				}
				return _sampled;
			}
		}

		public bool InWater => Current != null;

		public WaterChannel Channel => Current;

		public float BodyDensity
		{
			get
			{
				return _bodyDensity;
			}
			set
			{
				_bodyDensity = Mathf.Max(1f, value);
			}
		}

		public float LinearDrag
		{
			get
			{
				return _linearDrag;
			}
			set
			{
				_linearDrag = Mathf.Max(0f, value);
			}
		}

		private void Awake()
		{
			if (_rb == null)
			{
				_rb = GetComponent<Rigidbody>();
			}
		}

		public void EnterChannel(WaterChannel channel)
		{
			if (!(channel == null))
			{
				if (_channelRefs.TryGetValue(channel, out var value))
				{
					_channelRefs[channel] = value + 1;
				}
				else
				{
					_channelRefs.Add(channel, 1);
					_channelOrder.Add(channel);
				}
				_channel = channel;
			}
		}

		public void ExitChannel(WaterChannel channel)
		{
			if (channel == null || !_channelRefs.TryGetValue(channel, out var value))
			{
				return;
			}
			if (value > 1)
			{
				_channelRefs[channel] = value - 1;
				return;
			}
			_channelRefs.Remove(channel);
			_channelOrder.Remove(channel);
			if (_channel == channel)
			{
				_channel = LastActiveChannel();
			}
		}

		private WaterChannel LastActiveChannel()
		{
			for (int num = _channelOrder.Count - 1; num >= 0; num--)
			{
				WaterChannel waterChannel = _channelOrder[num];
				if (waterChannel != null && _channelRefs.ContainsKey(waterChannel))
				{
					return waterChannel;
				}
				_channelOrder.RemoveAt(num);
			}
			return null;
		}

		private void OnDisable()
		{
			_channel = null;
			_channelRefs.Clear();
			_channelOrder.Clear();
		}

		private void FixedUpdate()
		{
			if (!DrivenExternally)
			{
				ApplyBuoyancy(Time.fixedDeltaTime);
			}
		}

		public void ApplyBuoyancy(float dt, bool drawForceArrows = false)
		{
			if (_rb == null || _channel == null || _points == null || _points.Length == 0)
			{
				return;
			}
			if (!_comInit)
			{
				_rb.centerOfMass = new Vector3(0f, _centerOfMassY, 0f);
				_comInit = true;
			}
			int num = _points.Length;
			float num2 = Mathf.Abs(Physics.gravity.y);
			float waterDensity = _channel.WaterDensity;
			float num3 = ((DisplacementMass > 0f) ? DisplacementMass : _rb.mass) / Mathf.Max(1f, _bodyDensity) / (float)num;
			bool flag = false;
			float num4 = 0f;
			Vector3 vector = WaterNormal();
			for (int i = 0; i < num; i++)
			{
				Vector3 vector2 = base.transform.TransformPoint(_points[i]);
				if (!_channel.TryWaterHeight(vector2, out var surfaceY))
				{
					continue;
				}
				float num5 = surfaceY - vector2.y;
				if (!(num5 <= 0f))
				{
					flag = true;
					if (num5 > num4)
					{
						num4 = num5;
					}
					float num6 = Mathf.Clamp01(num5 / _maxDepth);
					float num7 = waterDensity * num2 * num3 * num6;
					_rb.AddForceAtPosition(vector * num7, vector2, ForceMode.Force);
					Vector3 pointVelocity = _rb.GetPointVelocity(vector2);
					float num8 = waterDensity * num3 * num6;
					_rb.AddForceAtPosition(-pointVelocity * (_linearDrag * num8), vector2, ForceMode.Force);
				}
			}
			if (flag && _angularDrag > 0f)
			{
				_rb.angularVelocity *= Mathf.Clamp01(1f - _angularDrag * dt);
			}
			if ((_diveBrake > 0f) & flag)
			{
				Vector3 linearVelocity = _rb.linearVelocity;
				if (linearVelocity.y < 0f)
				{
					float num9 = Mathf.Clamp01(num4 / _maxDepth);
					_rb.AddForce(Vector3.up * ((0f - linearVelocity.y) * _diveBrake * num9), ForceMode.Acceleration);
				}
			}
			if (_uprightTorque > 0f && (flag || _channel.TryWaterHeight(base.transform.position, out var _)))
			{
				Vector3 vector3 = Vector3.Cross(base.transform.up, vector);
				float num10 = Mathf.Clamp(vector3.magnitude, 0f, 1f);
				Vector3 vector4 = ((num10 > 0.0001f) ? (vector3 / num10 * (Mathf.Asin(num10) * _uprightTorque)) : Vector3.zero);
				Vector3 angularVelocity = _rb.angularVelocity;
				angularVelocity.y = 0f;
				_rb.AddTorque(vector4 - angularVelocity * _uprightDamping, ForceMode.Acceleration);
			}
			if (_maxAngularSpeed > 0f)
			{
				_rb.angularVelocity = Vector3.ClampMagnitude(_rb.angularVelocity, _maxAngularSpeed);
			}
		}

		private Vector3 WaterNormal()
		{
			Vector3 target = Vector3.up;
			if (_channel != null && !_channel.IsFlatSurface)
			{
				Vector3 position = base.transform.position;
				if (_channel.TryWaterHeight(position + new Vector3(2f, 0f, 0f), out var surfaceY) && _channel.TryWaterHeight(position + new Vector3(-2f, 0f, 0f), out var surfaceY2) && _channel.TryWaterHeight(position + new Vector3(0f, 0f, 2f), out var surfaceY3) && _channel.TryWaterHeight(position + new Vector3(0f, 0f, -2f), out var surfaceY4))
				{
					target = new Vector3((0f - (surfaceY - surfaceY2)) / 4f, 1f, (0f - (surfaceY3 - surfaceY4)) / 4f).normalized;
				}
			}
			target = Vector3.RotateTowards(Vector3.up, target, _maxSurfaceTilt * (MathF.PI / 180f), 0f);
			_smoothNormal = Vector3.Slerp(_smoothNormal, target, _normalSmooth).normalized;
			return _smoothNormal;
		}

		private void OnDrawGizmosSelected()
		{
			if (_points != null)
			{
				Gizmos.color = Color.cyan;
				for (int i = 0; i < _points.Length; i++)
				{
					Gizmos.DrawWireSphere(base.transform.TransformPoint(_points[i]), 0.08f);
				}
			}
		}
	}
}
