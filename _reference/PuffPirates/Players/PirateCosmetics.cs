using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates;
using PuffPirates.Cosmetics;
using PuffPirates.Networking;
using PuffPirates.Systems;
using TMPro;
using UnityEngine;

namespace PuffPirates.Players
{
	[RequireComponent(typeof(PiratePlayer))]
	[DisallowMultipleComponent]
	public class PirateCosmetics : TickNetworkBehaviour
	{
		private PiratePlayer _player;

		private CosmeticRelaySystem _cosmeticRelay;

		private int _boatSkinPushedForIndex = int.MinValue;

		public readonly SyncVar<int> _svHatId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svRobeId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svCharacterId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svEyewearId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svTopId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svBottomId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svShoesId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svSkinId = new SyncVar<int>(new SyncTypeSettings(0f));

		private Transform _nameTagTr;

		private TextMeshPro _nameLabel;

		private bool _nameTagInit;

		private string _shownName;

		private Camera _nameCam;

		private TextMeshPro _readyLabel;

		private TextMeshPro _localArrowLabel;

		private bool _lobbyIndicatorsInit;

		private int _appliedHat = int.MinValue;

		private int _appliedRobe = int.MinValue;

		private GameObject _hatGo;

		private GameObject _robeGo;

		private int _appliedEyewear = int.MinValue;

		private int _appliedTop = int.MinValue;

		private int _appliedBottom = int.MinValue;

		private GameObject _eyewearGo;

		private GameObject _topGo;

		private GameObject _bottomGo;

		private int _appliedChar = int.MinValue;

		private int _appliedCharBoatSize = int.MinValue;

		private GameObject _charGo;

		private int _appliedShoes = int.MinValue;

		private GameObject _shoesGo;

		private int _appliedSkin = int.MinValue;

		private SkinnedMeshRenderer _bodySmr;

		private Transform _skeletonRoot;

		private bool _riggedResolved;

		private bool _defCached;

		private float _defTop;

		private float _defRadius;

		private float _defBottom;

		private float _defHeight;

		private Transform _modelTransform;

		private const bool ShowGarments = false;

		private bool NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPirateCosmeticsAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EPlayers_002EPirateCosmeticsAssembly_002DCSharp_002Edll_Excuted;

		public int HatId
		{
			get
			{
				return _svHatId.Value;
			}
			private set
			{
				_svHatId.Value = value;
			}
		}

		public int RobeId
		{
			get
			{
				return _svRobeId.Value;
			}
			private set
			{
				_svRobeId.Value = value;
			}
		}

		public int CharacterId
		{
			get
			{
				return _svCharacterId.Value;
			}
			private set
			{
				_svCharacterId.Value = value;
			}
		}

		public int EyewearId
		{
			get
			{
				return _svEyewearId.Value;
			}
			private set
			{
				_svEyewearId.Value = value;
			}
		}

		public int TopId
		{
			get
			{
				return _svTopId.Value;
			}
			private set
			{
				_svTopId.Value = value;
			}
		}

		public int BottomId
		{
			get
			{
				return _svBottomId.Value;
			}
			private set
			{
				_svBottomId.Value = value;
			}
		}

		public int ShoesId
		{
			get
			{
				return _svShoesId.Value;
			}
			private set
			{
				_svShoesId.Value = value;
			}
		}

		public int SkinId
		{
			get
			{
				return _svSkinId.Value;
			}
			private set
			{
				_svSkinId.Value = value;
			}
		}

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EPlayers_002EPirateCosmetics_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		private CosmeticRelaySystem CosmeticRelay()
		{
			if (_cosmeticRelay == null)
			{
				this.TryGetSystem<CosmeticRelaySystem, CoreSystemHandler>(out _cosmeticRelay);
			}
			return _cosmeticRelay;
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (base.IsOwner)
			{
				RpcRequestCosmetic(0, CosmeticStore.Equipped(CosmeticSlot.Hat));
				RpcRequestCosmetic(1, CosmeticStore.Equipped(CosmeticSlot.Robe));
				RpcRequestCosmetic(2, CosmeticStore.Equipped(CosmeticSlot.BoatSkin));
				RpcRequestCosmetic(3, CosmeticStore.Equipped(CosmeticSlot.Character));
				RpcRequestCosmetic(11, CosmeticStore.Equipped(CosmeticSlot.Eyewear));
				RpcRequestCosmetic(12, CosmeticStore.Equipped(CosmeticSlot.Top));
				RpcRequestCosmetic(13, CosmeticStore.Equipped(CosmeticSlot.Bottom));
				RpcRequestCosmetic(14, CosmeticStore.Equipped(CosmeticSlot.Shoes));
				RpcRequestCosmetic(4, CosmeticStore.Equipped(CosmeticSlot.CharacterSkin));
			}
		}

		[ServerRpc]
		public void RpcRequestCosmetic(int slot, int id)
		{
			RpcWriter___Server_RpcRequestCosmetic___1692629761(slot, id);
		}

		public void SetCosmetic(CosmeticSlot slot, int id)
		{
			if (base.IsServerInitialized)
			{
				id = CosmeticCatalog.Clamp(slot, id);
				if (slot == CosmeticSlot.Hat)
				{
					HatId = id;
				}
				if (slot == CosmeticSlot.Robe)
				{
					RobeId = id;
				}
				if (slot == CosmeticSlot.Character)
				{
					CharacterId = id;
				}
				if (slot == CosmeticSlot.Eyewear)
				{
					EyewearId = id;
				}
				if (slot == CosmeticSlot.Top)
				{
					TopId = id;
				}
				if (slot == CosmeticSlot.Bottom)
				{
					BottomId = id;
				}
				if (slot == CosmeticSlot.Shoes)
				{
					ShoesId = id;
				}
				if (slot == CosmeticSlot.CharacterSkin)
				{
					SkinId = id;
				}
			}
		}

		private void UpdateNameTag()
		{
			if (!_nameTagInit)
			{
				Transform transform = base.transform.Find("NameTag");
				if (transform == null)
				{
					return;
				}
				_nameTagInit = true;
				_nameTagTr = transform;
				bool flag = base.IsSpawned && base.IsOwner;
				transform.gameObject.SetActive(!flag);
				if (!flag)
				{
					_nameLabel = transform.GetComponentInChildren<TextMeshPro>(includeInactive: true);
				}
			}
			if (!(_nameTagTr == null) && !(_nameLabel == null) && _nameTagTr.gameObject.activeSelf)
			{
				if (_player.PlayerName != _shownName)
				{
					_shownName = _player.PlayerName;
					string playerName = _player.PlayerName;
					_nameLabel.text = (string.IsNullOrEmpty(playerName) ? $"Player {base.OwnerId}" : playerName);
				}
				if (_nameCam == null || !_nameCam.isActiveAndEnabled)
				{
					_nameCam = Camera.main;
				}
				if (_nameCam != null)
				{
					_nameTagTr.rotation = Quaternion.LookRotation(_nameTagTr.position - _nameCam.transform.position, Vector3.up);
				}
			}
		}

		private void UpdateLobbyIndicators()
		{
			RaceState instance = RaceState.Instance;
			bool flag = instance != null && instance.InLobby && instance.CountdownRemaining <= 0.01f;
			if (!_lobbyIndicatorsInit && _nameTagTr != null)
			{
				_lobbyIndicatorsInit = true;
				_readyLabel = CreateWorldLabel("ReadyIcon", "✓", Color.green, 0.5f);
				if (base.IsSpawned && base.IsOwner)
				{
					_localArrowLabel = CreateWorldLabel("LocalArrow", "▼", Color.yellow, 0.8f);
				}
			}
			bool flag2 = flag && _player.IsReady;
			if (_readyLabel != null)
			{
				_readyLabel.gameObject.SetActive(flag2);
			}
			if (_localArrowLabel != null)
			{
				_localArrowLabel.gameObject.SetActive(flag);
			}
			if (!flag)
			{
				return;
			}
			if (_nameCam == null || !_nameCam.isActiveAndEnabled)
			{
				_nameCam = Camera.main;
			}
			if (!(_nameCam == null))
			{
				if (flag2)
				{
					BillboardTo(_readyLabel.transform);
				}
				if (_localArrowLabel != null)
				{
					BillboardTo(_localArrowLabel.transform);
				}
			}
		}

		private void BillboardTo(Transform t)
		{
			t.rotation = Quaternion.LookRotation(t.position - _nameCam.transform.position, Vector3.up);
		}

		private TextMeshPro CreateWorldLabel(string name, string text, Color color, float heightOffset)
		{
			GameObject obj = new GameObject(name);
			obj.transform.SetParent(_nameTagTr.parent, worldPositionStays: false);
			obj.transform.localPosition = _nameTagTr.localPosition + Vector3.up * heightOffset;
			TextMeshPro textMeshPro = obj.AddComponent<TextMeshPro>();
			textMeshPro.text = text;
			textMeshPro.color = color;
			textMeshPro.fontSize = 6f;
			textMeshPro.alignment = TextAlignmentOptions.Center;
			return textMeshPro;
		}

		protected override void TimeManager_OnUpdate()
		{
			if (base.IsClientInitialized)
			{
				UpdateNameTag();
				UpdateLobbyIndicators();
				if (_player.Boat != null && base.IsOwner && _boatSkinPushedForIndex != _player.BoatIndex)
				{
					_boatSkinPushedForIndex = _player.BoatIndex;
					RpcRequestCosmetic(2, CosmeticStore.Equipped(CosmeticSlot.BoatSkin));
				}
				ApplyCosmetics();
			}
		}

		private void ApplyCosmetics()
		{
			if (_modelTransform == null)
			{
				_modelTransform = base.transform.Find("Model");
			}
			Transform modelTransform = _modelTransform;
			Transform transform = ((modelTransform != null) ? modelTransform : base.transform);
			if (!_riggedResolved && modelTransform != null)
			{
				_riggedResolved = true;
				SkinnedMeshRenderer[] componentsInChildren = modelTransform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
				foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren)
				{
					if (!skinnedMeshRenderer.transform.name.StartsWith("~"))
					{
						_bodySmr = skinnedMeshRenderer;
						break;
					}
				}
				if (_bodySmr != null && _bodySmr.rootBone != null)
				{
					Transform transform2 = _bodySmr.rootBone;
					while (transform2.parent != null && transform2.parent != modelTransform)
					{
						transform2 = transform2.parent;
					}
					_skeletonRoot = transform2;
				}
			}
			if (_appliedSkin != SkinId)
			{
				CosmeticRelaySystem cosmeticRelaySystem = CosmeticRelay();
				if (cosmeticRelaySystem != null && _bodySmr != null)
				{
					cosmeticRelaySystem.ApplyBodySkin(_bodySmr, SkinId);
				}
				_appliedSkin = SkinId;
			}
			if (!_defCached)
			{
				MeasureModel(transform, out _defTop, out _defRadius, out _defBottom, out _defHeight);
				_defCached = true;
			}
			if (_appliedChar != CharacterId)
			{
				if (_charGo != null)
				{
					Object.Destroy(_charGo);
					_charGo = null;
				}
				Renderer[] componentsInChildren2 = transform.GetComponentsInChildren<Renderer>(includeInactive: true);
				foreach (Renderer renderer in componentsInChildren2)
				{
					if (!(renderer is ParticleSystemRenderer) && !(renderer is LineRenderer) && !renderer.transform.name.StartsWith("~"))
					{
						renderer.enabled = true;
					}
				}
				_appliedCharBoatSize = int.MinValue;
				_appliedChar = CharacterId;
				_appliedHat = int.MinValue;
				_appliedRobe = int.MinValue;
				_appliedEyewear = int.MinValue;
				_appliedTop = int.MinValue;
				_appliedBottom = int.MinValue;
				_appliedShoes = int.MinValue;
			}
			bool flag = CharacterId == 0 || CharacterId == 1;
			int num = ((_player.Boat != null) ? _player.Boat.SizeId : 2);
			if (((modelTransform != null) & flag) && _appliedCharBoatSize != num)
			{
				_appliedCharBoatSize = num;
				_appliedHat = int.MinValue;
				_appliedRobe = int.MinValue;
				_appliedEyewear = int.MinValue;
				_appliedTop = int.MinValue;
				_appliedBottom = int.MinValue;
			}
		}

		private bool TryApplySkinnedGarment(CosmeticSlot slot, int id, ref int applied, ref GameObject go, Transform model)
		{
			CosmeticRelaySystem cosmeticRelaySystem = CosmeticRelay();
			if (cosmeticRelaySystem == null || _skeletonRoot == null || model == null || !cosmeticRelaySystem.HasArt(slot))
			{
				return false;
			}
			if (applied != id)
			{
				if (go != null)
				{
					Object.Destroy(go);
				}
				go = cosmeticRelaySystem.BuildSkinned(slot, id, model, _skeletonRoot);
				applied = id;
			}
			return true;
		}

		private static void MeasureModel(Transform host, out float top, out float radius, out float bottom, out float height)
		{
			top = 1.5f;
			radius = 0.35f;
			bottom = 0f;
			height = 1.2f;
			Renderer[] componentsInChildren = host.GetComponentsInChildren<Renderer>(includeInactive: true);
			bool flag = false;
			Bounds bounds = default;
			Renderer[] array = componentsInChildren;
			foreach (Renderer renderer in array)
			{
				if (!(renderer is ParticleSystemRenderer) && !(renderer is LineRenderer) && !renderer.transform.name.StartsWith("~") && renderer.enabled)
				{
					if (!flag)
					{
						bounds = renderer.bounds;
						flag = true;
					}
					else
					{
						bounds.Encapsulate(renderer.bounds);
					}
				}
			}
			if (flag)
			{
				Vector3 vector = host.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
				Vector3 vector2 = host.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
				bottom = Mathf.Min(vector.y, vector2.y);
				top = Mathf.Max(vector.y, vector2.y);
				height = Mathf.Abs(top - bottom);
				radius = Mathf.Max(bounds.extents.x, bounds.extents.z) / Mathf.Max(0.0001f, host.lossyScale.x);
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPirateCosmeticsAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPirateCosmeticsAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_svSkinId.InitializeEarly(this, 7u, isSyncObject: false);
				_svShoesId.InitializeEarly(this, 6u, isSyncObject: false);
				_svBottomId.InitializeEarly(this, 5u, isSyncObject: false);
				_svTopId.InitializeEarly(this, 4u, isSyncObject: false);
				_svEyewearId.InitializeEarly(this, 3u, isSyncObject: false);
				_svCharacterId.InitializeEarly(this, 2u, isSyncObject: false);
				_svRobeId.InitializeEarly(this, 1u, isSyncObject: false);
				_svHatId.InitializeEarly(this, 0u, isSyncObject: false);
				RegisterServerRpc(0u, RpcReader___Server_RpcRequestCosmetic___1692629761);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EPlayers_002EPirateCosmeticsAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EPlayers_002EPirateCosmeticsAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_svSkinId.InitializeLate();
				_svShoesId.InitializeLate();
				_svBottomId.InitializeLate();
				_svTopId.InitializeLate();
				_svEyewearId.InitializeLate();
				_svCharacterId.InitializeLate();
				_svRobeId.InitializeLate();
				_svHatId.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void RpcWriter___Server_RpcRequestCosmetic___1692629761(int slot, int id)
		{
			if (!base.IsClientInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			if (!base.IsOwner)
			{
				NetworkManager networkManager2 = base.NetworkManager;
				networkManager2.LogWarning("Cannot complete action because you are not the owner of this object. .");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteInt32(slot);
			pooledWriter.WriteInt32(id);
			SendServerRpc(0u, pooledWriter, channel, DataOrderType.Default);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcRequestCosmetic___1692629761(int P_0, int P_1)
		{
			P_1 = CosmeticCatalog.Clamp((CosmeticSlot)P_0, P_1);
			switch (P_0)
			{
			case 0:
				HatId = P_1;
				break;
			case 1:
				RobeId = P_1;
				break;
			case 2:
				_player.Boat?.SetSkin(P_1);
				break;
			case 3:
				CharacterId = P_1;
				break;
			case 11:
				EyewearId = P_1;
				break;
			case 12:
				TopId = P_1;
				break;
			case 13:
				BottomId = P_1;
				break;
			case 14:
				ShoesId = P_1;
				break;
			case 4:
				SkinId = P_1;
				break;
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 10:
				break;
			}
		}

		private void RpcReader___Server_RpcRequestCosmetic___1692629761(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
		{
			int num = PooledReader0.ReadInt32();
			int num2 = PooledReader0.ReadInt32();
			if (base.IsServerInitialized && OwnerMatches(conn))
			{
				RpcLogic___RpcRequestCosmetic___1692629761(num, num2);
			}
		}

		private void Awake_UserLogic_PuffPirates_002EPlayers_002EPirateCosmetics_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Update);
			_player = GetComponent<PiratePlayer>();
		}
	}
}
