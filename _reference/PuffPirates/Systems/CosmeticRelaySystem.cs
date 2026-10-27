using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Localization;
using PuffPirates.Backend;
using PuffPirates.Cosmetics;
using PuffPirates.Players;
using UnityEngine;

namespace PuffPirates.Systems
{
	public class CosmeticRelaySystem : MonoBehaviour, IGameSystem
	{
		private readonly Dictionary<CosmeticSlot, int> _equipSeq = new Dictionary<CosmeticSlot, int>();

		[Header("Skinned garment FBX prefabları (id0=yok; id1.. giysi)")]
		[SerializeField]
		private GameObject[] _hat;

		[SerializeField]
		private GameObject[] _top;

		[SerializeField]
		private GameObject[] _bottom;

		[SerializeField]
		private GameObject[] _shoes;

		[SerializeField]
		private GameObject[] _eyewear;

		[Header("Karakterler (CharacterSkin id → mesh+materyal+ikonlar) — ScriptableObject asset")]
		[SerializeField]
		private CharacterDataConfig _characterData;

		private Dictionary<string, Transform> _boneMap;

		private Transform _boneMapRoot;

		public string SystemId => "CosmeticRelaySystem";

		public int CharacterCount
		{
			get
			{
				if (!(_characterData != null))
				{
					return 0;
				}
				return _characterData.Count;
			}
		}

		public IReadOnlyList<CharacterData> AllCharacters
		{
			get
			{
				if (!(_characterData != null))
				{
					return Array.Empty<CharacterData>();
				}
				return _characterData.All;
			}
		}

		public event Action Changed;

		public event Action<string> Toast;

		public UniTask Initialize()
		{
			CosmeticCatalog.RegisterCountSource(RealCount);
			return UniTask.CompletedTask;
		}

		private void OnDestroy()
		{
			CosmeticCatalog.UnregisterCountSource(RealCount);
		}

		private int RealCount(CosmeticSlot slot)
		{
			if (slot == CosmeticSlot.CharacterSkin)
			{
				return CharacterCount;
			}
			return 0;
		}

		public void Equip(CosmeticSlot slot, int id, PiratePlayer localPirate)
		{
			id = CosmeticCatalog.Clamp(slot, id);
			bool num = BackendBootstrap.Online && slot < CosmeticSlot.Eyewear;
			int previous = CosmeticStore.Equipped(slot);
			if (!num)
			{
				CosmeticStore.Grant(slot, id);
			}
			CosmeticStore.SetEquipped(slot, id);
			if (num)
			{
				_equipSeq.TryGetValue(slot, out var value);
				value = (_equipSeq[slot] = value + 1);
				EquipOnlineAsync(slot, id, previous, value);
			}
			if (slot == CosmeticSlot.Clothing)
			{
				Equip(CosmeticSlot.Hat, CosmeticCatalog.ClothingHat(id), localPirate);
				Equip(CosmeticSlot.Robe, CosmeticCatalog.ClothingRobe(id), localPirate);
			}
			if (localPirate != null)
			{
				localPirate.Cosmetics.RpcRequestCosmetic((int)slot, id);
			}
			Toast?.Invoke(CosmeticCatalog.Label(slot, id) ?? "");
			Changed?.Invoke();
		}

		private async Task EquipOnlineAsync(CosmeticSlot slot, int id, int previous, int seq)
		{
			try
			{
				FartTogetherApi.LoadoutResponse loadoutResponse = await BackendBootstrap.Api.EquipAsync((int)slot, id);
				if (_equipSeq[slot] != seq)
				{
					return;
				}
				if (loadoutResponse == null)
				{
					CosmeticStore.SetEquipped(slot, previous);
					Toast?.Invoke(Loc.Get("net.toast.equip_failed"));
					Changed?.Invoke();
					return;
				}
				int num = CosmeticCatalog.Clamp(slot, LoadoutEquippedFor(loadoutResponse, slot));
				if (num != CosmeticStore.Equipped(slot))
				{
					CosmeticStore.SetEquipped(slot, num);
					Changed?.Invoke();
				}
			}
			catch (Exception ex)
			{
				if (_equipSeq[slot] == seq)
				{
					CosmeticStore.SetEquipped(slot, previous);
					Debug.LogWarning("[CosmeticRelaySystem] /loadout/equip failed — equip rolled back: " + ex.Message);
					Toast?.Invoke(Loc.Get("net.toast.equip_failed"));
					Changed?.Invoke();
				}
			}
		}

		private static int LoadoutEquippedFor(FartTogetherApi.LoadoutResponse l, CosmeticSlot slot)
		{
			return slot switch
			{
				CosmeticSlot.Hat => l.hatId, 
				CosmeticSlot.Robe => l.robeId, 
				CosmeticSlot.BoatSkin => l.boatSkinId, 
				CosmeticSlot.Character => l.characterId, 
				CosmeticSlot.CharacterSkin => l.characterSkinId, 
				CosmeticSlot.CharacterPalette => l.characterPaletteId, 
				CosmeticSlot.Clothing => l.clothingId, 
				CosmeticSlot.ProfileIcon => l.profileIconId, 
				CosmeticSlot.BoatType => l.boatTypeId, 
				CosmeticSlot.BoatPalette => l.boatPaletteId, 
				CosmeticSlot.BoatAccessory => l.boatAccessoryId, 
				_ => CosmeticStore.Equipped(slot), 
			};
		}

		private GameObject[] ArrayFor(CosmeticSlot slot)
		{
			return slot switch
			{
				CosmeticSlot.Hat => _hat, 
				CosmeticSlot.Top => _top, 
				CosmeticSlot.Bottom => _bottom, 
				CosmeticSlot.Shoes => _shoes, 
				CosmeticSlot.Eyewear => _eyewear, 
				_ => null, 
			};
		}

		public bool HasArt(CosmeticSlot slot)
		{
			GameObject[] array = ArrayFor(slot);
			if (array != null)
			{
				return array.Length != 0;
			}
			return false;
		}

		public GameObject Build(CosmeticSlot slot, int id, Transform parent, float headTop, float bodyBottom, float bodyR, float bodyH)
		{
			GameObject[] array = slot switch
			{
				CosmeticSlot.Eyewear => _eyewear, 
				CosmeticSlot.Top => _top, 
				CosmeticSlot.Bottom => _bottom, 
				_ => null, 
			};
			if (array == null || id <= 0 || id >= array.Length || array[id] == null)
			{
				return null;
			}
			GameObject obj = UnityEngine.Object.Instantiate(array[id], parent, worldPositionStays: false);
			float y = ((slot == CosmeticSlot.Eyewear) ? (headTop * 0.9f) : (bodyBottom + bodyH * 0.5f));
			obj.transform.localPosition = new Vector3(0f, y, 0f);
			return obj;
		}

		public GameObject BuildSkinned(CosmeticSlot slot, int id, Transform model, Transform skeletonRoot)
		{
			GameObject[] array = ArrayFor(slot);
			if (array == null || id <= 0 || id >= array.Length || array[id] == null)
			{
				return null;
			}
			if (model == null || skeletonRoot == null)
			{
				return null;
			}
			if (_boneMap == null || _boneMapRoot != skeletonRoot)
			{
				_boneMap = SkinnedGarmentBinder.BuildBoneMap(skeletonRoot);
				_boneMapRoot = skeletonRoot;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(array[id]);
			SkinnedMeshRenderer componentInChildren = gameObject.GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
			if (componentInChildren == null)
			{
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			SkinnedGarmentBinder.Rebind(componentInChildren, _boneMap, skeletonRoot);
			GameObject gameObject2 = componentInChildren.gameObject;
			gameObject2.name = "~Garment_" + slot.ToString() + "_" + id;
			gameObject2.transform.SetParent(model, worldPositionStays: false);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			gameObject2.transform.localScale = Vector3.one;
			if (gameObject != gameObject2)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
			return gameObject2;
		}

		public void ApplyBodySkin(SkinnedMeshRenderer bodySmr, int skinId)
		{
			CharacterData characterData = GetCharacterData(skinId);
			if (!(bodySmr == null) && characterData != null)
			{
				if (characterData.Mesh != null)
				{
					bodySmr.sharedMesh = characterData.Mesh;
				}
				if (characterData.Materials != null)
				{
					Material[] sharedMaterials = bodySmr.sharedMaterials;
					sharedMaterials = characterData.Materials;
					bodySmr.sharedMaterials = sharedMaterials;
				}
			}
		}

		public CharacterData GetCharacterData(int skinId)
		{
			if (!(_characterData != null))
			{
				return null;
			}
			return _characterData.Get(skinId);
		}

		public Sprite GetLobbyIcon(int skinId)
		{
			return GetCharacterData(skinId)?.LobbyIcon;
		}

		public Sprite GetGameIcon(int skinId)
		{
			return GetCharacterData(skinId)?.GameIcon;
		}

		public Color? GetAccentColor(int skinId)
		{
			return GetCharacterData(skinId)?.AccentColor;
		}

		public int Count(CosmeticSlot slot)
		{
			return CosmeticCatalog.Count(slot);
		}

		public IEnumerable<int> AllIds(CosmeticSlot slot)
		{
			int n = Count(slot);
			for (int id = 0; id < n; id++)
			{
				yield return id;
			}
		}

		public string GetLabel(CosmeticSlot slot, int id)
		{
			return CosmeticCatalog.Label(slot, id);
		}

		public int Equipped(CosmeticSlot slot)
		{
			return CosmeticStore.Equipped(slot);
		}
	}
}
