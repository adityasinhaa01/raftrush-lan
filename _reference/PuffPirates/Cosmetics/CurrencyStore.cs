using UnityEngine;

namespace PuffPirates.Cosmetics
{
	public static class CurrencyStore
	{
		private const string Key = "pp_coins";

		private const string DKey = "pp_diamonds";

		private const int StartBalance = 9999;

		private const int StartDiamonds = 50;

		private static bool _online;

		private static long _cache;

		private static long _dCache;

		public static int Balance
		{
			get
			{
				if (!_online)
				{
					return PlayerPrefs.GetInt("pp_coins", 9999);
				}
				return ClampToInt(_cache);
			}
		}

		public static int Diamonds
		{
			get
			{
				if (!_online)
				{
					return PlayerPrefs.GetInt("pp_diamonds", 50);
				}
				return ClampToInt(_dCache);
			}
		}

		public static void Hydrate(long coins, long diamonds)
		{
			_cache = coins;
			_dCache = diamonds;
			_online = true;
		}

		public static void SetOffline()
		{
			_online = false;
		}

		public static void ResetOfflinePlaceholder()
		{
			PlayerPrefs.DeleteKey("pp_coins");
			PlayerPrefs.DeleteKey("pp_diamonds");
			PlayerPrefs.Save();
		}

		public static void SetBalancesFromServer(long coins, long diamonds)
		{
			_cache = coins;
			_dCache = diamonds;
		}

		public static void SetBalanceFromServer(long coins)
		{
			_cache = coins;
		}

		public static int BalanceOf(CurrencyKind c)
		{
			if (c != CurrencyKind.Diamonds)
			{
				return Balance;
			}
			return Diamonds;
		}

		public static bool CanAfford(int amount)
		{
			return Balance >= amount;
		}

		public static bool CanAfford(CurrencyKind c, int amount)
		{
			return BalanceOf(c) >= amount;
		}

		public static bool TrySpend(int amount)
		{
			return TrySpend(CurrencyKind.Coins, amount);
		}

		public static bool TrySpend(CurrencyKind c, int amount)
		{
			if (amount <= 0)
			{
				return true;
			}
			int num = BalanceOf(c);
			if (num < amount)
			{
				return false;
			}
			SetLocal(c, num - amount);
			return true;
		}

		public static void Add(int amount)
		{
			Add(CurrencyKind.Coins, amount);
		}

		public static void Add(CurrencyKind c, int amount)
		{
			if (amount > 0)
			{
				SetLocal(c, BalanceOf(c) + amount);
			}
		}

		private static void SetLocal(CurrencyKind c, long v)
		{
			if (c == CurrencyKind.Diamonds)
			{
				_dCache = v;
			}
			else
			{
				_cache = v;
			}
			if (!_online)
			{
				if (c == CurrencyKind.Diamonds)
				{
					PlayerPrefs.SetInt("pp_diamonds", ClampToInt(v));
				}
				else
				{
					PlayerPrefs.SetInt("pp_coins", ClampToInt(v));
				}
				PlayerPrefs.Save();
			}
		}

		private static int ClampToInt(long v)
		{
			if (v > int.MaxValue)
			{
				Debug.LogWarning($"[CurrencyStore] bakiye int sınırını aştı ({v}) — {int.MaxValue} olarak sınırlandı.");
				return int.MaxValue;
			}
			if (v < int.MinValue)
			{
				return int.MinValue;
			}
			return (int)v;
		}
	}
}
