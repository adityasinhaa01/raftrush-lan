using System;
using UnityEngine;

namespace PuffPirates.Cosmetics
{
	[Serializable]
	public class CharacterData
	{
		public int Id;

		public Mesh Mesh;

		public Material[] Materials;

		public Sprite LobbyIcon;

		public Sprite GameIcon;

		public Color AccentColor = Color.white;
	}
}
