using PuffPirates.Players;
using PuffPirates.Systems;
using TMPro;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Lobby
{
	public class LobbyCharacterView : MonoBehaviour
	{
		[Header("Name tags — two separate objects (different size/sprite), never both on")]
		[Tooltip("Shown ONLY on the local player's own character (the \"You\" tag).")]
		[SerializeField]
		private GameObject _localTag;

		[Tooltip("Shown on every OTHER player's character — carries the real name.")]
		[SerializeField]
		private GameObject _remoteTag;

		[Tooltip("The name text inside the remote tag.")]
		[SerializeField]
		private TMP_Text _nameLabel;

		[Header("Ready indicator")]
		[SerializeField]
		private GameObject _readyBadge;

		private PiratePlayer _pirate;

		private CosmeticRelaySystem _cosmetics;

		private SkinnedMeshRenderer _bodySmr;

		private bool _bodyResolved;

		private int _appliedSkin = int.MinValue;

		private string _shownName;

		private bool _shownReady;

		private bool _readyStateKnown;

		private bool _tagsApplied;

		public PiratePlayer Pirate => _pirate;

		public void Bind(PiratePlayer pirate, CosmeticRelaySystem cosmetics)
		{
			_pirate = pirate;
			_cosmetics = cosmetics;
			_appliedSkin = int.MinValue;
			_shownName = null;
			_readyStateKnown = false;
			_tagsApplied = false;
		}

		public void Refresh()
		{
			if (!(_pirate == null))
			{
				ApplySkin();
				ApplyTags();
				ApplyReady();
			}
		}

		private void ApplySkin()
		{
			CosmeticRelaySystem cosmetics = _cosmetics;
			PirateCosmetics cosmetics2 = _pirate.Cosmetics;
			if (cosmetics == null || cosmetics2 == null)
			{
				return;
			}
			if (!_bodyResolved)
			{
				_bodyResolved = true;
				SkinnedMeshRenderer[] componentsInChildren = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
				foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren)
				{
					if (!skinnedMeshRenderer.transform.name.StartsWith("~"))
					{
						_bodySmr = skinnedMeshRenderer;
						break;
					}
				}
			}
			if (!(_bodySmr == null) && _appliedSkin != cosmetics2.SkinId)
			{
				cosmetics.ApplyBodySkin(_bodySmr, cosmetics2.SkinId);
				_appliedSkin = cosmetics2.SkinId;
			}
		}

		private void ApplyTags()
		{
			bool isSpawned = _pirate.IsSpawned;
			bool flag = isSpawned && _pirate.IsOwner;
			if (!_tagsApplied)
			{
				_tagsApplied = true;
				if (_localTag != null)
				{
					_localTag.SetActive(flag);
				}
				if (_remoteTag != null)
				{
					_remoteTag.SetActive(!flag);
				}
			}
			if (!flag && !(_nameLabel == null))
			{
				string text = _pirate.PlayerName;
				if (string.IsNullOrEmpty(text))
				{
					text = (isSpawned ? ("Player " + _pirate.OwnerId) : "Player");
				}
				if (!(text == _shownName))
				{
					_shownName = text;
					_nameLabel.text = text;
				}
			}
		}

		private void ApplyReady()
		{
			if (!(_readyBadge == null))
			{
				bool isReady = _pirate.IsReady;
				if (!_readyStateKnown || isReady != _shownReady)
				{
					_readyStateKnown = true;
					_shownReady = isReady;
					_readyBadge.SetActive(isReady);
				}
			}
		}
	}
}
