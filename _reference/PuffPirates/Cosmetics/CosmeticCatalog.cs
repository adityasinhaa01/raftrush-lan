using System;
using GamesMrkt.Localization;
using PuffPirates.Visuals;
using UnityEngine;

namespace PuffPirates.Cosmetics
{
	public static class CosmeticCatalog
	{
		private static Func<CosmeticSlot, int> _countSource;

		private static readonly Color[] _hatColors = new Color[6]
		{
			Color.clear,
			new Color(0.85f, 0.15f, 0.15f),
			new Color(0.15f, 0.35f, 0.85f),
			new Color(0.95f, 0.8f, 0.15f),
			new Color(0.15f, 0.15f, 0.15f),
			new Color(0.2f, 0.7f, 0.35f)
		};

		private static readonly Color[] _robeColors = new Color[5]
		{
			Color.clear,
			new Color(0.6f, 0.2f, 0.7f),
			new Color(0.9f, 0.9f, 0.95f),
			new Color(0.8f, 0.45f, 0.1f),
			new Color(0.1f, 0.55f, 0.6f)
		};

		private static readonly Color[] _skinColors = new Color[5]
		{
			new Color(0.85f, 0.88f, 0.92f),
			new Color(0.9f, 0.3f, 0.3f),
			new Color(0.3f, 0.6f, 0.95f),
			new Color(0.2f, 0.2f, 0.25f),
			new Color(0.95f, 0.75f, 0.2f)
		};

		private static readonly Color[] _characterColors = new Color[4]
		{
			new Color(0.85f, 0.72f, 0.55f),
			new Color(0.55f, 0.35f, 0.18f),
			new Color(0.95f, 0.95f, 0.98f),
			new Color(0.9f, 0.92f, 0.96f)
		};

		private static readonly Color[] _charSkinColors = new Color[4]
		{
			new Color(0.85f, 0.72f, 0.55f),
			new Color(0.6f, 0.45f, 0.3f),
			new Color(0.45f, 0.55f, 0.65f),
			new Color(0.72f, 0.45f, 0.42f)
		};

		private static readonly Color[] _charPaletteColors = new Color[5]
		{
			new Color(0.85f, 0.72f, 0.55f),
			new Color(0.95f, 0.35f, 0.35f),
			new Color(0.35f, 0.85f, 0.55f),
			new Color(0.45f, 0.55f, 0.95f),
			new Color(0.95f, 0.8f, 0.3f)
		};

		private static readonly int[] _clothingHat = new int[5] { 0, 1, 2, 4, 3 };

		private static readonly int[] _clothingRobe = new int[5] { 0, 1, 3, 2, 1 };

		private static readonly Color[] _profileIconColors = new Color[6]
		{
			new Color(0.45f, 0.55f, 0.7f),
			new Color(0.9f, 0.4f, 0.4f),
			new Color(0.4f, 0.75f, 0.95f),
			new Color(0.55f, 0.85f, 0.5f),
			new Color(0.8f, 0.55f, 0.95f),
			new Color(0.95f, 0.8f, 0.35f)
		};

		private static readonly Color[] _boatTypeColors = new Color[4]
		{
			new Color(0.72f, 0.55f, 0.35f),
			new Color(0.35f, 0.62f, 0.72f),
			new Color(0.85f, 0.3f, 0.35f),
			new Color(0.3f, 0.3f, 0.38f)
		};

		private static readonly Color[] _boatPaletteColors = new Color[5]
		{
			new Color(0.85f, 0.88f, 0.92f),
			new Color(0.95f, 0.45f, 0.2f),
			new Color(0.2f, 0.85f, 0.8f),
			new Color(0.6f, 0.35f, 0.85f),
			new Color(0.3f, 0.9f, 0.4f)
		};

		private static readonly Color[] _boatAccessoryColors = new Color[5]
		{
			new Color(0.4f, 0.4f, 0.45f),
			new Color(0.9f, 0.25f, 0.3f),
			new Color(0.98f, 0.85f, 0.45f),
			new Color(0.92f, 0.92f, 0.96f),
			new Color(0.95f, 0.55f, 0.65f)
		};

		private static readonly Color[] _eyewearColors = new Color[4]
		{
			Color.clear,
			new Color(0.12f, 0.12f, 0.14f),
			new Color(0.35f, 0.55f, 0.85f),
			new Color(0.85f, 0.75f, 0.25f)
		};

		private static readonly Color[] _topColors = new Color[5]
		{
			Color.clear,
			new Color(0.9f, 0.9f, 0.92f),
			new Color(0.2f, 0.45f, 0.8f),
			new Color(0.85f, 0.25f, 0.3f),
			new Color(0.2f, 0.65f, 0.4f)
		};

		private static readonly Color[] _bottomColors = new Color[4]
		{
			Color.clear,
			new Color(0.2f, 0.22f, 0.28f),
			new Color(0.55f, 0.35f, 0.2f),
			new Color(0.85f, 0.85f, 0.9f)
		};

		private static readonly Color[] _shoesColors = new Color[2]
		{
			Color.clear,
			new Color(0.35f, 0.25f, 0.18f)
		};

		public static void RegisterCountSource(Func<CosmeticSlot, int> source)
		{
			_countSource = source;
		}

		public static void UnregisterCountSource(Func<CosmeticSlot, int> source)
		{
			if (_countSource == source)
			{
				_countSource = null;
			}
		}

		public static int Count(CosmeticSlot slot)
		{
			if (_countSource != null)
			{
				int num = _countSource(slot);
				if (num > 0)
				{
					return num;
				}
			}
			return PlaceholderCount(slot);
		}

		private static int PlaceholderCount(CosmeticSlot slot)
		{
			return slot switch
			{
				CosmeticSlot.Hat => _hatColors.Length, 
				CosmeticSlot.Robe => _robeColors.Length, 
				CosmeticSlot.BoatSkin => _skinColors.Length, 
				CosmeticSlot.Character => _characterColors.Length, 
				CosmeticSlot.CharacterSkin => _charSkinColors.Length, 
				CosmeticSlot.CharacterPalette => _charPaletteColors.Length, 
				CosmeticSlot.Clothing => _clothingHat.Length, 
				CosmeticSlot.ProfileIcon => _profileIconColors.Length, 
				CosmeticSlot.BoatType => _boatTypeColors.Length, 
				CosmeticSlot.BoatPalette => _boatPaletteColors.Length, 
				CosmeticSlot.BoatAccessory => _boatAccessoryColors.Length, 
				CosmeticSlot.Eyewear => _eyewearColors.Length, 
				CosmeticSlot.Top => _topColors.Length, 
				CosmeticSlot.Bottom => _bottomColors.Length, 
				CosmeticSlot.Shoes => _shoesColors.Length, 
				_ => 1, 
			};
		}

		public static int Clamp(CosmeticSlot slot, int id)
		{
			int num = Count(slot);
			return (id % num + num) % num;
		}

		private static int PlaceholderIndex(CosmeticSlot slot, int id)
		{
			int num = PlaceholderCount(slot);
			return (id % num + num) % num;
		}

		public static string Label(CosmeticSlot slot, int id)
		{
			id = Clamp(slot, id);
			return slot switch
			{
				CosmeticSlot.Hat => (id == 0) ? Loc.Get("cosmetic.none") : Loc.Get("cosmetic.fallback.hat", ("id", id)), 
				CosmeticSlot.Robe => (id == 0) ? Loc.Get("cosmetic.none") : Loc.Get("cosmetic.fallback.robe", ("id", id)), 
				CosmeticSlot.BoatSkin => (id == 0) ? Loc.Get("cosmetic.default") : Loc.Get("cosmetic.fallback.boat_skin", ("id", id)), 
				CosmeticSlot.Character => id switch
				{
					0 => Loc.Get("cosmetic.default"), 
					1 => Loc.Get("cosmetic.character.bear"), 
					2 => Loc.Get("cosmetic.character.panda"), 
					3 => Loc.Get("cosmetic.character.polar_bear"), 
					_ => Loc.Get("cosmetic.fallback.character", ("id", id)), 
				}, 
				CosmeticSlot.CharacterSkin => id switch
				{
					0 => Loc.Get("cosmetic.character_skin.classic"), 
					1 => Loc.Get("cosmetic.character_skin.pirate"), 
					2 => Loc.Get("cosmetic.character_skin.ninja"), 
					3 => Loc.Get("cosmetic.character_skin.robot"), 
					4 => Loc.Get("cosmetic.character_skin.ghost"), 
					_ => Loc.Get("cosmetic.fallback.character_skin", ("id", id)), 
				}, 
				CosmeticSlot.CharacterPalette => (id == 0) ? Loc.Get("cosmetic.character_palette.natural") : Loc.Get("cosmetic.fallback.character_palette", ("id", id)), 
				CosmeticSlot.Clothing => id switch
				{
					0 => Loc.Get("cosmetic.clothing.casual"), 
					1 => Loc.Get("cosmetic.clothing.captain"), 
					2 => Loc.Get("cosmetic.clothing.sailor"), 
					3 => Loc.Get("cosmetic.clothing.royal"), 
					4 => Loc.Get("cosmetic.clothing.shadow"), 
					_ => Loc.Get("cosmetic.fallback.clothing", ("id", id)), 
				}, 
				CosmeticSlot.ProfileIcon => (id == 0) ? Loc.Get("cosmetic.default") : Loc.Get("cosmetic.fallback.profile_icon", ("id", id)), 
				CosmeticSlot.BoatType => id switch
				{
					0 => Loc.Get("cosmetic.boat_type.boat"), 
					1 => Loc.Get("cosmetic.boat_type.catamaran"), 
					2 => Loc.Get("cosmetic.boat_type.speed_boat"), 
					3 => Loc.Get("cosmetic.boat_type.galley"), 
					_ => Loc.Get("cosmetic.fallback.boat_type", ("id", id)), 
				}, 
				CosmeticSlot.BoatPalette => (id == 0) ? Loc.Get("cosmetic.boat_palette.standard") : Loc.Get("cosmetic.fallback.boat_palette", ("id", id)), 
				CosmeticSlot.BoatAccessory => id switch
				{
					0 => Loc.Get("cosmetic.none"), 
					1 => Loc.Get("cosmetic.boat_accessory.flag"), 
					2 => Loc.Get("cosmetic.boat_accessory.lantern"), 
					3 => Loc.Get("cosmetic.boat_accessory.sail"), 
					4 => Loc.Get("cosmetic.boat_accessory.balloon"), 
					_ => Loc.Get("cosmetic.fallback.boat_accessory", ("id", id)), 
				}, 
				CosmeticSlot.Eyewear => (id == 0) ? Loc.Get("cosmetic.none") : Loc.Get("cosmetic.fallback.eyewear", ("id", id)), 
				CosmeticSlot.Top => (id == 0) ? Loc.Get("cosmetic.none") : Loc.Get("cosmetic.fallback.top", ("id", id)), 
				CosmeticSlot.Bottom => (id == 0) ? Loc.Get("cosmetic.none") : Loc.Get("cosmetic.fallback.bottom", ("id", id)), 
				CosmeticSlot.Shoes => (id == 0) ? Loc.Get("cosmetic.none") : ("Ayakkabı " + id), 
				_ => id.ToString(), 
			};
		}

		public static CurrencyKind CurrencyOf(CosmeticSlot slot, int id)
		{
			if (RarityOf(slot, id) != Rarity.Efsanevi)
			{
				return CurrencyKind.Coins;
			}
			return CurrencyKind.Diamonds;
		}

		public static int Price(CosmeticSlot slot, int id)
		{
			id = Clamp(slot, id);
			if (id == 0)
			{
				return 0;
			}
			if (CurrencyOf(slot, id) == CurrencyKind.Diamonds)
			{
				return 20 + 5 * (id - 1);
			}
			return RarityOf(slot, id) switch
			{
				Rarity.Epik => 600, 
				Rarity.Nadir => 300, 
				_ => 150, 
			} + 25 * (id - 1);
		}

		public static Color RobeColor(int id)
		{
			return _robeColors[PlaceholderIndex(CosmeticSlot.Robe, id)];
		}

		public static Color BoatSkinColor(int id)
		{
			return _skinColors[PlaceholderIndex(CosmeticSlot.BoatSkin, id)];
		}

		public static Color SwatchColor(CosmeticSlot slot, int id)
		{
			id = PlaceholderIndex(slot, id);
			Color result = slot switch
			{
				CosmeticSlot.Hat => _hatColors[id], 
				CosmeticSlot.Robe => _robeColors[id], 
				CosmeticSlot.BoatSkin => _skinColors[id], 
				CosmeticSlot.Character => _characterColors[id], 
				CosmeticSlot.CharacterSkin => _charSkinColors[id], 
				CosmeticSlot.CharacterPalette => _charPaletteColors[id], 
				CosmeticSlot.Clothing => _robeColors[PlaceholderIndex(CosmeticSlot.Robe, _clothingRobe[id])], 
				CosmeticSlot.ProfileIcon => _profileIconColors[id], 
				CosmeticSlot.BoatType => _boatTypeColors[id], 
				CosmeticSlot.BoatPalette => _boatPaletteColors[id], 
				CosmeticSlot.BoatAccessory => _boatAccessoryColors[id], 
				CosmeticSlot.Eyewear => _eyewearColors[id], 
				CosmeticSlot.Top => _topColors[id], 
				CosmeticSlot.Bottom => _bottomColors[id], 
				CosmeticSlot.Shoes => _shoesColors[id], 
				_ => Color.grey, 
			};
			if (!(result.a < 0.01f))
			{
				return result;
			}
			return new Color(0.4f, 0.4f, 0.45f);
		}

		public static int ClothingHat(int id)
		{
			return _clothingHat[PlaceholderIndex(CosmeticSlot.Clothing, id)];
		}

		public static int ClothingRobe(int id)
		{
			return _clothingRobe[PlaceholderIndex(CosmeticSlot.Clothing, id)];
		}

		public static Rarity RarityOf(CosmeticSlot slot, int id)
		{
			id = Clamp(slot, id);
			if (id == 0)
			{
				return Rarity.Standart;
			}
			int num = Count(slot);
			if (id >= num - 1 && num >= 3)
			{
				return Rarity.Efsanevi;
			}
			if (id < 3)
			{
				return Rarity.Nadir;
			}
			return Rarity.Epik;
		}

		public static Color RarityColor(Rarity r)
		{
			return r switch
			{
				Rarity.Nadir => new Color(0.28f, 0.66f, 1f), 
				Rarity.Epik => new Color(0.7f, 0.43f, 1f), 
				Rarity.Efsanevi => new Color(1f, 0.78f, 0.31f), 
				_ => new Color(0.58f, 0.63f, 0.7f), 
			};
		}

		public static string RarityLabel(Rarity r)
		{
			return r switch
			{
				Rarity.Nadir => Loc.Get("cosmetic.rarity.nadir"), 
				Rarity.Epik => Loc.Get("cosmetic.rarity.epik"), 
				Rarity.Efsanevi => Loc.Get("cosmetic.rarity.efsanevi"), 
				_ => Loc.Get("cosmetic.rarity.standart"), 
			};
		}

		public static string Glyph(CosmeticSlot slot)
		{
			return slot switch
			{
				CosmeticSlot.Character => "\ud83d\udc3b", 
				CosmeticSlot.CharacterSkin => "\ud83c\udfad", 
				CosmeticSlot.CharacterPalette => "\ud83c\udfa8", 
				CosmeticSlot.Clothing => "\ud83d\udc55", 
				CosmeticSlot.ProfileIcon => "\ud83d\uddbc", 
				CosmeticSlot.BoatType => "⛵", 
				CosmeticSlot.BoatSkin => "\ud83d\udea4", 
				CosmeticSlot.BoatPalette => "\ud83c\udf08", 
				CosmeticSlot.BoatAccessory => "\ud83c\udf8f", 
				CosmeticSlot.Hat => "\ud83c\udfa9", 
				CosmeticSlot.Eyewear => "\ud83d\udc53", 
				CosmeticSlot.Top => "\ud83d\udc55", 
				CosmeticSlot.Bottom => "\ud83e\ude72", 
				CosmeticSlot.Shoes => "\ud83d\udc5f", 
				_ => "\ud83c\udf81", 
			};
		}

		public static GameObject BuildHat(int id, Transform parent, float headRadius)
		{
			id = Clamp(CosmeticSlot.Hat, id);
			if (id == 0)
			{
				return null;
			}
			GameObject gameObject = new GameObject($"~Hat_{id}");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			Color c = _hatColors[PlaceholderIndex(CosmeticSlot.Hat, id)];
			float num = Mathf.Max(0.05f, headRadius);
			if (id == 3)
			{
				GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				Strip(gameObject2);
				gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
				gameObject2.transform.localScale = new Vector3(num * 0.1f, num * 1.8f, num * 0.1f);
				gameObject2.transform.localPosition = new Vector3(0f, num * 1.8f, 0f);
				Paint(gameObject2, c);
				return gameObject;
			}
			GameObject gameObject3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			Strip(gameObject3);
			gameObject3.transform.SetParent(gameObject.transform, worldPositionStays: false);
			gameObject3.transform.localScale = new Vector3(num * 1.6f, num * 1.2f, num * 1.6f);
			gameObject3.transform.localPosition = new Vector3(0f, num * 1.2f, 0f);
			Paint(gameObject3, c);
			GameObject gameObject4 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			Strip(gameObject4);
			gameObject4.transform.SetParent(gameObject.transform, worldPositionStays: false);
			gameObject4.transform.localScale = new Vector3(num * 3f, num * 0.08f, num * 3f);
			gameObject4.transform.localPosition = new Vector3(0f, num * 0.1f, 0f);
			Paint(gameObject4, c);
			return gameObject;
		}

		public static GameObject BuildRobe(int id, Transform parent, float bodyRadius, float bodyHeight)
		{
			id = Clamp(CosmeticSlot.Robe, id);
			if (id == 0)
			{
				return null;
			}
			GameObject gameObject = new GameObject($"~Robe_{id}");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			Strip(gameObject2);
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
			float num = Mathf.Max(0.05f, bodyRadius) * 1.25f;
			float y = Mathf.Max(0.1f, bodyHeight) * 0.5f;
			gameObject2.transform.localScale = new Vector3(num, y, num);
			gameObject2.transform.localPosition = new Vector3(0f, y, 0f);
			Paint(gameObject2, _robeColors[PlaceholderIndex(CosmeticSlot.Robe, id)]);
			return gameObject;
		}

		public static GameObject BuildCharacter(int id, Transform parent, float height, float radius)
		{
			id = Clamp(CosmeticSlot.Character, id);
			if (id == 0 || id == 1)
			{
				return null;
			}
			GameObject gameObject = new GameObject($"CharModel_{id}");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			Color color;
			Color c;
			Color c2;
			Color c3;
			if (id == 2)
			{
				color = Color.white;
				c = Color.white;
				c2 = Color.black;
				c3 = Color.black;
			}
			else
			{
				color = new Color(0.93f, 0.95f, 0.98f);
				c = color;
				c2 = new Color(0.8f, 0.82f, 0.88f);
				c3 = new Color(0.3f, 0.3f, 0.35f);
			}
			float num = Mathf.Max(0.1f, radius);
			float num2 = Mathf.Max(0.3f, height);
			Sphere(gameObject.transform, new Vector3(0f, num2 * 0.35f, 0f), Vector3.one * (num * 1.7f), color);
			Sphere(gameObject.transform, new Vector3(0f, num2 * 0.8f, 0f), Vector3.one * (num * 1.3f), c);
			Sphere(gameObject.transform, new Vector3((0f - num) * 0.6f, num2 * 1.02f, 0f), Vector3.one * (num * 0.5f), c2);
			Sphere(gameObject.transform, new Vector3(num * 0.6f, num2 * 1.02f, 0f), Vector3.one * (num * 0.5f), c2);
			Sphere(gameObject.transform, new Vector3(0f, num2 * 0.72f, num * 0.8f), Vector3.one * (num * 0.55f), c3);
			if (id == 2)
			{
				Sphere(gameObject.transform, new Vector3((0f - num) * 0.35f, num2 * 0.85f, num * 0.7f), Vector3.one * (num * 0.35f), Color.black);
				Sphere(gameObject.transform, new Vector3(num * 0.35f, num2 * 0.85f, num * 0.7f), Vector3.one * (num * 0.35f), Color.black);
			}
			return gameObject;
		}

		public static GameObject BuildGarment(CosmeticSlot slot, int id, Transform parent, float headTop, float bodyBottom, float bodyR, float bodyH)
		{
			id = Clamp(slot, id);
			if (id == 0)
			{
				return null;
			}
			Color c = SwatchColor(slot, id);
			float num = Mathf.Max(0.05f, bodyR);
			GameObject gameObject = new GameObject($"~Garment_{slot}_{id}");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			switch (slot)
			{
			case CosmeticSlot.Eyewear:
			{
				float y3 = Mathf.Max(0.1f, headTop) * 0.85f;
				float num2 = Mathf.Max(0.04f, Mathf.Max(0.1f, headTop) * 0.12f);
				float z = num * 0.9f;
				Sphere(gameObject.transform, new Vector3((0f - num2) * 1.3f, y3, z), new Vector3(num2, num2, num2 * 0.4f), c);
				Sphere(gameObject.transform, new Vector3(num2 * 1.3f, y3, z), new Vector3(num2, num2, num2 * 0.4f), c);
				GameObject gameObject4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Strip(gameObject4);
				gameObject4.transform.SetParent(gameObject.transform, worldPositionStays: false);
				gameObject4.transform.localScale = new Vector3(num2 * 1.2f, num2 * 0.25f, num2 * 0.25f);
				gameObject4.transform.localPosition = new Vector3(0f, y3, z);
				Paint(gameObject4, c);
				break;
			}
			case CosmeticSlot.Top:
			{
				GameObject gameObject3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				Strip(gameObject3);
				gameObject3.transform.SetParent(gameObject.transform, worldPositionStays: false);
				float y2 = Mathf.Max(0.1f, bodyH) * 0.42f;
				gameObject3.transform.localScale = new Vector3(num * 1.15f, y2, num * 1.15f);
				gameObject3.transform.localPosition = new Vector3(0f, bodyBottom + bodyH * 0.55f, 0f);
				Paint(gameObject3, c);
				break;
			}
			case CosmeticSlot.Bottom:
			{
				GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				Strip(gameObject2);
				gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
				float y = Mathf.Max(0.1f, bodyH) * 0.2f;
				gameObject2.transform.localScale = new Vector3(num * 1.08f, y, num * 1.08f);
				gameObject2.transform.localPosition = new Vector3(0f, bodyBottom + bodyH * 0.18f, 0f);
				Paint(gameObject2, c);
				break;
			}
			default:
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			return gameObject;
		}

		private static GameObject Sphere(Transform parent, Vector3 pos, Vector3 scale, Color c)
		{
			GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			Strip(gameObject);
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			gameObject.transform.localPosition = pos;
			gameObject.transform.localScale = scale;
			Paint(gameObject, c);
			return gameObject;
		}

		private static void Strip(GameObject go)
		{
			Collider component = go.GetComponent<Collider>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
		}

		private static void Paint(GameObject go, Color c)
		{
			Renderer component = go.GetComponent<Renderer>();
			if (component != null)
			{
				RendererColorUtility.SetColor(component, c);
			}
		}
	}
}
