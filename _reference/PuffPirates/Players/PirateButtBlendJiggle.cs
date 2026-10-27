using System;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Players
{
	[DefaultExecutionOrder(-50)]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(PiratePlayer))]
	public sealed class PirateButtBlendJiggle : MonoBehaviour
	{
		private const float SideFreq2 = 2.17f;

		private const float SidePhase = 0.7f;

		private const float SideMix = 0.35f;

		private const float SideNorm = 1.35f;

		private const float VertFreq = 1.43f;

		private const float VertPhase = 1.1f;

		private const float BackFreq = 1.81f;

		private const float BackPhase = 0.35f;

		private const float BackBase = 0.45f;

		private const float BackSwing = 0.55f;

		private const float InflateFreq = 1.67f;

		private const float InflatePhase = 0.2f;

		private const float InflateBase = 0.5f;

		private const float InflateSwing = 0.5f;

		private const float PhaseWrap = 16f;

		private const float MaskWeightEps = 0.01f;

		private const float ActiveEps = 0.001f;

		private const float Tau = MathF.PI * 2f;

		[Tooltip("Tuning data (butt mask, gas vibration/swell, reddening, movement sway). Shared/versioned as an asset instead of living directly on the prefab.")]
		[SerializeField]
		private PirateButtBlendJiggleConfig _config;

		private PiratePlayer _player;

		private SkinnedMeshRenderer _renderer;

		private Transform _butBone;

		private int _shapeX = -1;

		private int _shapeY = -1;

		private int _shapeZ = -1;

		private int _shapeInflate = -1;

		private bool _ready;

		private Mesh _builtMesh;

		private Mesh _ownedMesh;

		private MaterialPropertyBlock _redMpb;

		private float _lastPaintedEnvelope = -1f;

		private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

		private float _gasEnvelope;

		private float _phase;

		private Vector3 _moveOffset;

		private Vector3 _moveVelocity;

		private Vector3 _lastBoatPos;

		private Vector3 _lastBoatVel;

		private float _lastBoatYaw;

		private bool _boatKinematicsInit;

		private void Awake()
		{
			_player = GetComponent<PiratePlayer>();
			_renderer = GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
			if (_renderer == null || _renderer.sharedMesh == null)
			{
				Debug.LogWarning("PirateButtBlendJiggle: no SkinnedMeshRenderer/mesh; jiggle disabled.", this);
				base.enabled = false;
				return;
			}
			if (_config == null)
			{
				Debug.LogError("[PirateButtBlendJiggle] No PirateButtBlendJiggleConfig assigned on '" + base.name + "' — jiggle disabled.", this);
				base.enabled = false;
				return;
			}
			Transform[] bones = _renderer.bones;
			for (int i = 0; i < bones.Length; i++)
			{
				if (bones[i] != null && bones[i].name == "ButBone")
				{
					_butBone = bones[i];
					break;
				}
			}
		}

		private bool BuildBlendShapes(Mesh baseMesh)
		{
			if (!baseMesh.isReadable)
			{
				_builtMesh = baseMesh;
				return false;
			}
			Vector3[] vertices = baseMesh.vertices;
			Vector3[] normals = baseMesh.normals;
			int num = vertices.Length;
			bool flag = normals != null && normals.Length == num;
			Vector3 vector = new Vector3(Mathf.Max(0.001f, _config.MaskRadii.x), Mathf.Max(0.001f, _config.MaskRadii.y), Mathf.Max(0.001f, _config.MaskRadii.z));
			Vector3[] array = new Vector3[num];
			Vector3[] array2 = new Vector3[num];
			Vector3[] array3 = new Vector3[num];
			Vector3[] array4 = new Vector3[num];
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				Vector3 vector2 = vertices[i] - _config.MaskCenter;
				float value = Mathf.Sqrt(vector2.x * vector2.x / (vector.x * vector.x) + vector2.y * vector2.y / (vector.y * vector.y) + vector2.z * vector2.z / (vector.z * vector.z));
				float num3 = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
				if (num3 > 0.01f)
				{
					num2++;
				}
				float shapeUnit = _config.ShapeUnit;
				array[i] = new Vector3(shapeUnit * num3, 0f, 0f);
				array2[i] = new Vector3(0f, shapeUnit * num3, 0f);
				array3[i] = new Vector3(0f, 0f, shapeUnit * num3);
				Vector3 vector3 = (flag ? normals[i] : Vector3.up);
				array4[i] = vector3 * (shapeUnit * num3);
			}
			if (num2 == 0)
			{
				_builtMesh = baseMesh;
				return false;
			}
			if (_ownedMesh != null)
			{
				UnityEngine.Object.Destroy(_ownedMesh);
			}
			Mesh mesh = UnityEngine.Object.Instantiate(baseMesh);
			mesh.name = baseMesh.name + "_buttBlend";
			mesh.AddBlendShapeFrame("ButtX", 100f, array, null, null);
			mesh.AddBlendShapeFrame("ButtY", 100f, array2, null, null);
			mesh.AddBlendShapeFrame("ButtZ", 100f, array3, null, null);
			mesh.AddBlendShapeFrame("ButtInflate", 100f, array4, null, null);
			_renderer.sharedMesh = mesh;
			_shapeX = mesh.GetBlendShapeIndex("ButtX");
			_shapeY = mesh.GetBlendShapeIndex("ButtY");
			_shapeZ = mesh.GetBlendShapeIndex("ButtZ");
			_shapeInflate = mesh.GetBlendShapeIndex("ButtInflate");
			_builtMesh = mesh;
			_ownedMesh = mesh;
			_lastPaintedEnvelope = -1f;
			if (_shapeX >= 0 && _shapeY >= 0 && _shapeZ >= 0)
			{
				return _shapeInflate >= 0;
			}
			return false;
		}

		private void LateUpdate()
		{
			if (_player == null || _renderer == null)
			{
				return;
			}
			Mesh sharedMesh = _renderer.sharedMesh;
			if (sharedMesh == null)
			{
				return;
			}
			if (sharedMesh != _builtMesh)
			{
				_ready = BuildBlendShapes(sharedMesh);
			}
			float deltaTime = Time.deltaTime;
			bool isGassing = _player.Gas.IsGassing;
			float t = Mathf.InverseLerp(100f, 6000f, _player.Gas.GasFxForce);
			float b = (isGassing ? Mathf.Lerp(_config.MinGasStrength, 1f, t) : 0f);
			_gasEnvelope = Mathf.Lerp(_gasEnvelope, b, 1f - Mathf.Exp((0f - (isGassing ? _config.AttackSpeed : _config.ReleaseSpeed)) * deltaTime));
			if (_butBone != null)
			{
				if (_config.BoneSwellEnabled)
				{
					float num = Mathf.Clamp01(_config.BoneSwellCurve.Evaluate(_player.Gas.GasPowerFraction));
					float num2 = ((num > 0.001f && _config.GasTrembleAmplitude > 0f) ? (1f + Mathf.Sin(Time.time * _config.GasTrembleFrequency * (MathF.PI * 2f)) * _config.GasTrembleAmplitude * num) : 1f);
					_butBone.localScale = Vector3.Lerp(Vector3.one, _config.GasPeakScale, num) * num2;
				}
				else
				{
					_butBone.localScale = Vector3.one;
				}
			}
			if (_ready)
			{
				if (_config.RednessEnabled)
				{
					PaintButtRedTiling();
				}
				else
				{
					ResetRedTiling();
				}
				Vector3 vector = Vector3.zero;
				float num3 = 0f;
				if (_config.GasJiggleEnabled && _gasEnvelope > 0.001f)
				{
					float num4 = Mathf.Lerp(_config.MinFrequency, _config.MaxFrequency, t);
					_phase = Mathf.Repeat(_phase + deltaTime * num4 * (MathF.PI * 2f), MathF.PI * 32f);
					float num5 = (Mathf.Sin(_phase) + 0.35f * Mathf.Sin(_phase * 2.17f + 0.7f)) / 1.35f;
					float num6 = Mathf.Sin(_phase * 1.43f + 1.1f);
					float num7 = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(_phase * 1.81f + 0.35f));
					float num8 = 0.5f + 0.5f * Mathf.Sin(_phase * 1.67f + 0.2f);
					vector = new Vector3(num5 * _config.SideAmplitude * _gasEnvelope, num6 * _config.VerticalAmplitude * _gasEnvelope, 0f - num7 * _config.BackAmplitude * _gasEnvelope);
					num3 = (_config.BalloonAmplitude + num8 * _config.InflateAmplitude) * _gasEnvelope;
				}
				if (_config.MovementSwayEnabled)
				{
					UpdateMovementOffset(deltaTime);
				}
				else
				{
					_moveOffset = Vector3.zero;
					_moveVelocity = Vector3.zero;
					_boatKinematicsInit = false;
				}
				Vector3 vector2 = vector + _moveOffset;
				float num9 = 100f / Mathf.Max(0.0001f, _config.ShapeUnit);
				_renderer.SetBlendShapeWeight(_shapeX, vector2.x * num9);
				_renderer.SetBlendShapeWeight(_shapeY, vector2.y * num9);
				_renderer.SetBlendShapeWeight(_shapeZ, vector2.z * num9);
				_renderer.SetBlendShapeWeight(_shapeInflate, num3 * num9);
			}
		}

		private void PaintButtRedTiling()
		{
			if (_renderer == null || _player == null)
			{
				return;
			}
			float value = ((_player.Gas.MaxGas > 0.001f) ? (_player.Gas.GasEnergy / _player.Gas.MaxGas) : 0f);
			float time = 1f - Mathf.Clamp01(value);
			float num = Mathf.Clamp01(_config.RedCurve.Evaluate(time));
			if (!(Mathf.Abs(num - _lastPaintedEnvelope) < 0.004f))
			{
				_lastPaintedEnvelope = num;
				if (_redMpb == null)
				{
					_redMpb = new MaterialPropertyBlock();
				}
				Vector2 vector = Vector2.Lerp(_config.RedTilingMin, _config.RedTilingMax, num);
				_renderer.GetPropertyBlock(_redMpb, _config.RedMaterialIndex);
				_redMpb.SetVector(BaseMapStId, new Vector4(vector.x, vector.y, 0f, 0f));
				_renderer.SetPropertyBlock(_redMpb, _config.RedMaterialIndex);
			}
		}

		private void ResetRedTiling()
		{
			if (!(_renderer == null) && _lastPaintedEnvelope != 0f)
			{
				_lastPaintedEnvelope = 0f;
				if (_redMpb == null)
				{
					_redMpb = new MaterialPropertyBlock();
				}
				_renderer.GetPropertyBlock(_redMpb, _config.RedMaterialIndex);
				_redMpb.SetVector(BaseMapStId, new Vector4(_config.RedTilingMin.x, _config.RedTilingMin.y, 0f, 0f));
				_renderer.SetPropertyBlock(_redMpb, _config.RedMaterialIndex);
			}
		}

		private void UpdateMovementOffset(float dt)
		{
			if (dt <= 0f)
			{
				return;
			}
			NetworkBoat boat = _player.Boat;
			Vector3 vector = Vector3.zero;
			if (boat != null)
			{
				Transform obj = boat.transform;
				Vector3 position = obj.position;
				float y = obj.eulerAngles.y;
				if (!_boatKinematicsInit)
				{
					_lastBoatPos = position;
					_lastBoatVel = Vector3.zero;
					_lastBoatYaw = y;
					_boatKinematicsInit = true;
				}
				Vector3 vector2 = (position - _lastBoatPos) / dt;
				Vector3 direction = (vector2 - _lastBoatVel) / dt;
				float num = Mathf.DeltaAngle(_lastBoatYaw, y) / dt;
				_lastBoatPos = position;
				_lastBoatVel = vector2;
				_lastBoatYaw = y;
				vector = -base.transform.InverseTransformDirection(direction) * _config.MoveInertia;
				vector.x += num * (MathF.PI / 180f) * _config.TurnSway;
				vector = Vector3.ClampMagnitude(vector, _config.MoveMaxOffset);
			}
			else
			{
				_boatKinematicsInit = false;
			}
			_moveVelocity += (vector - _moveOffset) * (_config.MoveStiffness * dt);
			_moveVelocity /= 1f + _config.MoveDamping * dt;
			_moveOffset += _moveVelocity * dt;
			_moveOffset = Vector3.ClampMagnitude(_moveOffset, _config.MoveMaxOffset * 1.5f);
		}

		private void OnDestroy()
		{
			if (_ownedMesh != null)
			{
				UnityEngine.Object.Destroy(_ownedMesh);
			}
		}

		private void OnDisable()
		{
			if (_butBone != null)
			{
				_butBone.localScale = Vector3.one;
			}
			if (_ready && !(_renderer == null))
			{
				_renderer.SetBlendShapeWeight(_shapeX, 0f);
				_renderer.SetBlendShapeWeight(_shapeY, 0f);
				_renderer.SetBlendShapeWeight(_shapeZ, 0f);
				_renderer.SetBlendShapeWeight(_shapeInflate, 0f);
				_lastPaintedEnvelope = -1f;
				if (_redMpb == null)
				{
					_redMpb = new MaterialPropertyBlock();
				}
				_renderer.GetPropertyBlock(_redMpb, _config.RedMaterialIndex);
				_redMpb.SetVector(BaseMapStId, new Vector4(_config.RedTilingMin.x, _config.RedTilingMin.y, 0f, 0f));
				_renderer.SetPropertyBlock(_redMpb, _config.RedMaterialIndex);
			}
		}
	}
}
