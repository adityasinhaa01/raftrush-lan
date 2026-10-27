using System;
using System.Collections.Generic;
using System.Text;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates;
using PuffPirates.Audio;
using PuffPirates.Boats;
using PuffPirates.CameraRig;
using PuffPirates.Juice;
using PuffPirates.Networking;
using PuffPirates.Systems;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class ButtonDoorObstacle : PhysicsObstacleBase
	{
		private sealed class Leaf
		{
			public Transform T;

			public Quaternion Closed;

			public Quaternion Open;
		}

		[Header("Yer tutucu modu")]
		[SerializeField]
		private Transform _doorHinge;

		[SerializeField]
		private Transform _buttonPad;

		[Header("Ortak")]
		[SerializeField]
		private Collider _pressTrigger;

		[SerializeField]
		[Min(0.05f)]
		private float _animDuration = 0.7f;

		[SerializeField]
		private float _openAngleY = 90f;

		[SerializeField]
		private float _buttonSink = 0.22f;

		[Tooltip("Tuşa basılınca kameranın kapıya baktığı süre (sn; giriş/çıkış geçişleri hariç). Kapı animasyonunu (~0.7 sn) kapsamalı.")]
		[SerializeField]
		[Min(0f)]
		private float _cameraHoldSeconds = 1.4f;

		[Tooltip("Kapı odağında kameranın kapıya uzaklığı (m). ~12 m: 8 m'lik kapı açıklığı ekrana sığar.")]
		[SerializeField]
		[Min(3f)]
		private float _cameraDistance = 12f;

		[Header("FBX modeli (boş bırakılırsa yer tutucu modu)")]
		[SerializeField]
		private GameObject _doorModel;

		[SerializeField]
		private GameObject _buttonModel;

		[Tooltip("Kapı modelinin doğacağı nokta. FBX kapının açıklığı kendi Z ekseninde; prefab'ın geçiş yönüne göre bu noktayı döndürün (ButtonDoor: Y=90). Boşsa bu obje.")]
		[SerializeField]
		private Transform _doorAnchor;

		[Tooltip("Tuş modelinin doğacağı nokta (PressTrigger bunun üstünde durmalı).")]
		[SerializeField]
		private Transform _buttonAnchor;

		[Tooltip("Kanat düğümlerinin ad önekleri (kanat + '_Col...' collider düğümü aynı önekle bulunur).")]
		[SerializeField]
		private string[] _leafPrefixes = new string[2] { "GM_3D_Obstacle_Door_L", "GM_3D_Obstacle_Door_R" };

		[Tooltip("Basılan tuş düğümünün TAM adı ('_Part' taban düğümü değil).")]
		[SerializeField]
		private string _buttonPadNodeName = "GM_3D_Obstacle_Door_Button";

		private readonly List<Leaf> _leaves = new List<Leaf>();

		private Quaternion _doorClosed;

		private Quaternion _doorOpen;

		private Vector3 _buttonUp;

		private Vector3 _buttonDown;

		private float _pressTime = -1f;

		private static readonly List<ButtonDoorObstacle> Registry = new List<ButtonDoorObstacle>();

		private int _doorId = -1;

		private int _idCount = -1;

		private const float DoorFocusHeight = 2.1f;

		protected override void Awake()
		{
			base.Awake();
			SetupModel();
			if (_doorHinge != null)
			{
				_doorClosed = _doorHinge.localRotation;
				_doorOpen = Quaternion.Euler(0f, _openAngleY, 0f) * _doorClosed;
			}
			if (_buttonPad != null)
			{
				_buttonUp = _buttonPad.localPosition;
				_buttonDown = _buttonUp + Vector3.down * _buttonSink;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (!Registry.Contains(this))
			{
				Registry.Add(this);
			}
			_idCount = -1;
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Registry.Remove(this);
		}

		private static string PathKey(Transform t)
		{
			StringBuilder stringBuilder = new StringBuilder();
			while (t != null)
			{
				stringBuilder.Insert(0, t.GetSiblingIndex().ToString("D5") + "/");
				t = t.parent;
			}
			return stringBuilder.ToString();
		}

		private int DoorId()
		{
			if (_idCount != Registry.Count)
			{
				List<ButtonDoorObstacle> list = new List<ButtonDoorObstacle>(Registry);
				list.Sort((ButtonDoorObstacle a, ButtonDoorObstacle b) => string.CompareOrdinal(PathKey(a.transform), PathKey(b.transform)));
				for (int num = 0; num < list.Count; num++)
				{
					list[num]._doorId = num;
					list[num]._idCount = Registry.Count;
				}
			}
			return _doorId;
		}

		private void SetupModel()
		{
			if (_leafPrefixes == null || _leafPrefixes.Length == 0)
			{
				_leafPrefixes = new string[2] { "GM_3D_Obstacle_Door_L", "GM_3D_Obstacle_Door_R" };
			}
			if (string.IsNullOrEmpty(_buttonPadNodeName))
			{
				_buttonPadNodeName = "GM_3D_Obstacle_Door_Button";
			}
			Transform transform = ((_doorAnchor != null) ? _doorAnchor : base.transform);
			if (_doorModel != null && transform.Find(_doorModel.name) == null)
			{
				GameObject obj = UnityEngine.Object.Instantiate(_doorModel, transform);
				obj.name = _doorModel.name;
				obj.transform.localPosition = Vector3.zero;
				obj.transform.localRotation = Quaternion.identity;
			}
			if (_buttonModel != null)
			{
				Transform transform2 = ((_buttonAnchor != null) ? _buttonAnchor : base.transform);
				if (transform2.Find(_buttonModel.name) == null)
				{
					GameObject obj2 = UnityEngine.Object.Instantiate(_buttonModel, transform2);
					obj2.name = _buttonModel.name;
					obj2.transform.localPosition = Vector3.zero;
					obj2.transform.localRotation = Quaternion.identity;
				}
			}
			Animator[] componentsInChildren = GetComponentsInChildren<Animator>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			Transform[] componentsInChildren2 = GetComponentsInChildren<Transform>(includeInactive: true);
			Transform[] array = componentsInChildren2;
			foreach (Transform transform3 in array)
			{
				if (transform3.name == _buttonPadNodeName)
				{
					_buttonPad = transform3;
					break;
				}
			}
			Vector3 away = AwayFromButton();
			string[] leafPrefixes = _leafPrefixes;
			foreach (string value in leafPrefixes)
			{
				Transform transform4 = null;
				List<Transform> list = new List<Transform>();
				array = componentsInChildren2;
				foreach (Transform transform5 in array)
				{
					if (transform5.name.StartsWith(value))
					{
						list.Add(transform5);
						if (transform5.name.IndexOf("Col", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							MakeColliderNode(transform5);
						}
						else
						{
							transform4 = transform5;
						}
					}
				}
				if (transform4 == null)
				{
					continue;
				}
				float num = OpenSign(transform4, away);
				Quaternion quaternion = Quaternion.Euler(0f, num * _openAngleY, 0f);
				foreach (Transform item in list)
				{
					_leaves.Add(new Leaf
					{
						T = item,
						Closed = item.localRotation,
						Open = quaternion * item.localRotation
					});
				}
			}
			Debug.Log($"[ButtonDoor] '{base.name}' setup: leaves={_leaves.Count} pad={_buttonPad != null} doorModel={_doorModel != null} nodes={componentsInChildren2.Length}");
		}

		private static void MakeColliderNode(Transform t)
		{
			Renderer component = t.GetComponent<Renderer>();
			if (component != null)
			{
				component.enabled = false;
			}
			MeshFilter component2 = t.GetComponent<MeshFilter>();
			if (!(component2 == null) && !(component2.sharedMesh == null) && !(t.GetComponent<Collider>() != null))
			{
				BoxCollider boxCollider = t.gameObject.AddComponent<BoxCollider>();
				boxCollider.center = component2.sharedMesh.bounds.center;
				boxCollider.size = component2.sharedMesh.bounds.size;
			}
		}

		private Vector3 AwayFromButton()
		{
			if (_pressTrigger == null)
			{
				return Vector3.left;
			}
			Vector3 vector = base.transform.InverseTransformPoint(_pressTrigger.transform.position);
			vector.y = 0f;
			if (!(vector.sqrMagnitude < 0.0001f))
			{
				return -vector.normalized;
			}
			return Vector3.left;
		}

		private float OpenSign(Transform leaf, Vector3 away)
		{
			Renderer component = leaf.GetComponent<Renderer>();
			Vector3 vector = base.transform.InverseTransformPoint(leaf.position);
			Vector3 vector2 = base.transform.InverseTransformPoint((component != null) ? component.bounds.center : leaf.position) - vector;
			vector2.y = 0f;
			float num = Vector3.Dot(Quaternion.Euler(0f, _openAngleY, 0f) * vector2, away);
			float num2 = Vector3.Dot(Quaternion.Euler(0f, 0f - _openAngleY, 0f) * vector2, away);
			if (!(num >= num2))
			{
				return -1f;
			}
			return 1f;
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			RaceState instance = RaceState.Instance;
			int num = DoorId();
			if (instance != null && num >= 0 && num < instance.DoorPressTick.Count)
			{
				if (instance.IsServerInitialized && instance.DoorPressTick[num] == 0 && IsBoatInTrigger())
				{
					instance.PressDoor(num, tick);
				}
				int num2 = instance.DoorPressTick[num];
				if (num2 == 0)
				{
					if (_pressTime >= 0f)
					{
						ResetPose();
					}
					return;
				}
				if (_pressTime < 0f)
				{
					bool flag = time - (float)num2 * dt > _animDuration + 0.5f;
					_pressTime = (flag ? (time - _animDuration) : time);
					Debug.Log($"[ButtonDoor] '{base.name}' id={num} press tick={num2} (now {tick}) late={flag} host={instance.IsServerInitialized}");
					if (!flag)
					{
						OnPressedFeedback();
					}
				}
			}
			else if (_pressTime < 0f && IsBoatInTrigger())
			{
				_pressTime = time;
				OnPressedFeedback();
			}
			if (!(_pressTime < 0f))
			{
				float num3 = ((_animDuration <= 0f) ? 1f : Mathf.Clamp01((time - _pressTime) / _animDuration));
				num3 = num3 * num3 * (3f - 2f * num3);
				Pose(num3);
			}
		}

		private void OnPressedFeedback()
		{
			Vector3 position = ((_buttonPad != null) ? _buttonPad.position : base.transform.position);
			Vector3 vector = ((_doorAnchor != null) ? _doorAnchor : base.transform).position + Vector3.up * 2.1f;
			Sfx3D.Play(CommonSound.ButtonClick, position);
			Sfx3D.Play(CommonSound.DoorCreak, vector);
			BoatCameraRig.Shake(0.25f);
			HapticManager.Medium();
			BoatCameraRig.FocusOn(vector, _cameraHoldSeconds, _cameraDistance);
			((Singleton<CoreSystemHandler>.Instance != null) ? Singleton<CoreSystemHandler>.Instance.GetSystem<ConnectionSystem>() : null)?.RaiseToast("Kapı açıldı!");
		}

		private void Pose(float u)
		{
			if (_doorHinge != null)
			{
				_doorHinge.localRotation = Quaternion.Slerp(_doorClosed, _doorOpen, u);
			}
			for (int i = 0; i < _leaves.Count; i++)
			{
				_leaves[i].T.localRotation = Quaternion.Slerp(_leaves[i].Closed, _leaves[i].Open, u);
			}
			if (_buttonPad != null)
			{
				_buttonPad.localPosition = Vector3.Lerp(_buttonUp, _buttonDown, u);
			}
		}

		private void ResetPose()
		{
			_pressTime = -1f;
			Pose(0f);
		}

		private bool IsBoatInTrigger()
		{
			if (_pressTrigger == null)
			{
				return false;
			}
			List<NetworkBoat> all = NetworkBoat.All;
			for (int i = 0; i < all.Count; i++)
			{
				NetworkBoat networkBoat = all[i];
				if (networkBoat == null)
				{
					continue;
				}
				Vector3 position = networkBoat.transform.position;
				if (_pressTrigger is BoxCollider boxCollider)
				{
					Vector3 vector = _pressTrigger.transform.InverseTransformPoint(position) - boxCollider.center;
					Vector3 vector2 = boxCollider.size * 0.5f;
					vector2.y = Mathf.Max(vector2.y, 2f);
					if (Mathf.Abs(vector.x) <= vector2.x && Mathf.Abs(vector.y) <= vector2.y && Mathf.Abs(vector.z) <= vector2.z)
					{
						return true;
					}
				}
				else if ((_pressTrigger.ClosestPoint(position) - position).sqrMagnitude < 0.0001f)
				{
					return true;
				}
			}
			return false;
		}
	}
}
