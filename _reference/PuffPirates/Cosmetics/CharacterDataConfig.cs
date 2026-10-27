using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Cosmetics
{
	[CreateAssetMenu(fileName = "CharacterDataConfig", menuName = "PuffPirates/Cosmetics/Character Data Config")]
	public class CharacterDataConfig : ScriptableObject
	{
		[SerializeField]
		private CharacterData[] _characters;

		public int Count
		{
			get
			{
				CharacterData[] characters = _characters;
				if (characters == null)
				{
					return 0;
				}
				return characters.Length;
			}
		}

		public IReadOnlyList<CharacterData> All => _characters ?? Array.Empty<CharacterData>();

		public CharacterData Get(int id)
		{
			if (_characters == null || _characters.Length == 0)
			{
				return null;
			}
			int num = _characters.Length;
			int num2 = (id % num + num) % num;
			return _characters[num2];
		}
	}
}
