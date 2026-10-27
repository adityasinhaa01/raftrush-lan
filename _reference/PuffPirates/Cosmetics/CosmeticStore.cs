using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Cosmetics
{
	public static class CosmeticStore
	{
		private static bool _online;

		private static readonly Dictionary<CosmeticSlot, HashSet<int>> _owned = new Dictionary<CosmeticSlot, HashSet<int>>();

		private static readonly Dictionary<CosmeticSlot, int> _equipped = new Dictionary<CosmeticSlot, int>();

		private static string OwnedKey(CosmeticSlot s)
		{
			return s switch
			{
				CosmeticSlot.Hat => "pp_owned_hats", 
				CosmeticSlot.Robe => "pp_owned_robes", 
				CosmeticSlot.BoatSkin => "pp_owned_skins", 
				CosmeticSlot.Character => "pp_owned_chars", 
				CosmeticSlot.CharacterSkin => "pp_owned_cskin", 
				CosmeticSlot.CharacterPalette => "pp_owned_cpal", 
				CosmeticSlot.Clothing => "pp_owned_cloth", 
				CosmeticSlot.ProfileIcon => "pp_owned_picon", 
				CosmeticSlot.BoatType => "pp_owned_btype", 
				CosmeticSlot.BoatPalette => "pp_owned_bpal", 
				CosmeticSlot.Eyewear => "pp_owned_eyewear", 
				CosmeticSlot.Top => "pp_owned_top", 
				CosmeticSlot.Bottom => "pp_owned_bottom", 
				CosmeticSlot.Shoes => "pp_owned_shoes", 
				_ => "pp_owned_bacc", 
			};
		}

		private static string EqKey(CosmeticSlot s)
		{
			return s switch
			{
				CosmeticSlot.Hat => "pp_eq_hat", 
				CosmeticSlot.Robe => "pp_eq_robe", 
				CosmeticSlot.BoatSkin => "pp_eq_skin", 
				CosmeticSlot.Character => "pp_eq_char", 
				CosmeticSlot.CharacterSkin => "pp_eq_cskin", 
				CosmeticSlot.CharacterPalette => "pp_eq_cpal", 
				CosmeticSlot.Clothing => "pp_eq_cloth", 
				CosmeticSlot.ProfileIcon => "pp_eq_picon", 
				CosmeticSlot.BoatType => "pp_eq_btype", 
				CosmeticSlot.BoatPalette => "pp_eq_bpal", 
				CosmeticSlot.Eyewear => "pp_eq_eyewear", 
				CosmeticSlot.Top => "pp_eq_top", 
				CosmeticSlot.Bottom => "pp_eq_bottom", 
				CosmeticSlot.Shoes => "pp_eq_shoes", 
				_ => "pp_eq_bacc", 
			};
		}

		public static void Hydrate(List<KeyValuePair<int, int>> inventory, int[] loadoutBySlot)
		{
			_online = true;
			_owned.Clear();
			_equipped.Clear();
			for (int i = 0; i <= 14; i++)
			{
				CosmeticSlot key = (CosmeticSlot)i;
				_owned[key] = new HashSet<int> { 0 };
				_equipped[key] = ((loadoutBySlot != null && i < loadoutBySlot.Length) ? loadoutBySlot[i] : 0);
			}
			if (inventory == null)
			{
				return;
			}
			foreach (KeyValuePair<int, int> item in inventory)
			{
				CosmeticSlot key2 = (CosmeticSlot)item.Key;
				if (_owned.TryGetValue(key2, out var value))
				{
					value.Add(CosmeticCatalog.Clamp(key2, item.Value));
				}
			}
		}

		public static void SetOffline()
		{
			_online = false;
		}

		public static HashSet<int> Owned(CosmeticSlot slot)
		{
			if (_online)
			{
				if (!_owned.TryGetValue(slot, out var value))
				{
					return new HashSet<int> { 0 };
				}
				return new HashSet<int>(value);
			}
			HashSet<int> hashSet = new HashSet<int> { 0 };
			string[] array = PlayerPrefs.GetString(OwnedKey(slot), "0").Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				if (int.TryParse(array[i], out var result))
				{
					hashSet.Add(CosmeticCatalog.Clamp(slot, result));
				}
			}
			return hashSet;
		}

		public static bool IsOwned(CosmeticSlot slot, int id)
		{
			id = CosmeticCatalog.Clamp(slot, id);
			if (_online)
			{
				if (id != 0)
				{
					if (_owned.TryGetValue(slot, out var value))
					{
						return value.Contains(id);
					}
					return false;
				}
				return true;
			}
			return Owned(slot).Contains(id);
		}

		public static void Grant(CosmeticSlot slot, int id)
		{
			id = CosmeticCatalog.Clamp(slot, id);
			if (_online)
			{
				if (_owned.TryGetValue(slot, out var value))
				{
					value.Add(id);
					return;
				}
				_owned[slot] = new HashSet<int> { 0, id };
			}
			else
			{
				HashSet<int> hashSet = Owned(slot);
				if (hashSet.Add(id))
				{
					PlayerPrefs.SetString(OwnedKey(slot), string.Join(",", hashSet));
					PlayerPrefs.Save();
				}
			}
		}

		public static int Equipped(CosmeticSlot slot)
		{
			if (_online)
			{
				if (!_equipped.TryGetValue(slot, out var value))
				{
					return 0;
				}
				return CosmeticCatalog.Clamp(slot, value);
			}
			return CosmeticCatalog.Clamp(slot, PlayerPrefs.GetInt(EqKey(slot), 0));
		}

		public static void SetEquipped(CosmeticSlot slot, int id)
		{
			id = CosmeticCatalog.Clamp(slot, id);
			if (_online)
			{
				_equipped[slot] = id;
				return;
			}
			PlayerPrefs.SetInt(EqKey(slot), id);
			PlayerPrefs.Save();
		}
	}
}
