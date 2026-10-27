using System.Collections.Generic;
using GamesMrkt.Core;
using PuffPirates.Players;
using PuffPirates.Systems;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Lobby
{
	public class LobbyCharacterStage : MonoBehaviour
	{
		[Header("Character shown on an occupied seat (lobby_character prefab)")]
		[SerializeField]
		private LobbyCharacterView _characterPrefab;

		[Header("Seat points — 8 of them, placed by hand in the scene")]
		[SerializeField]
		private Transform[] _seatPoints;

		[Header("Fallback layout — only used when no seat point is assigned above")]
		[SerializeField]
		private int _fallbackSeatCount = 8;

		[SerializeField]
		private float _fallbackSpacing = 1.1f;

		[Tooltip("Y rotation given to generated seats. The character model faces +Z, the lobby camera looks from -Z -> 180.")]
		[SerializeField]
		private float _fallbackFacing = 180f;

		private CosmeticRelaySystem _cosmetics;

		private LobbyCharacterView[] _seatViews;

		private readonly List<PiratePlayer> _seated = new List<PiratePlayer>();

		private void Awake()
		{
			BuildFallbackSeatsIfNeeded();
			_seatViews = new LobbyCharacterView[(_seatPoints != null) ? _seatPoints.Length : 0];
			if (!(Singleton<CoreSystemHandler>.Instance != null))
			{
				return;
			}
			this.WhenSystemReady<CosmeticRelaySystem, CoreSystemHandler>((CosmeticRelaySystem system) =>
			{
				if (this != null)
				{
					_cosmetics = system;
				}
			});
		}

		private void Update()
		{
			if (_characterPrefab == null || _seatViews == null || _seatViews.Length == 0)
			{
				return;
			}
			ReleaseEmptiedSeats();
			SeatNewPlayers();
			for (int i = 0; i < _seatViews.Length; i++)
			{
				if (_seatViews[i] != null)
				{
					_seatViews[i].Refresh();
				}
			}
		}

		private void ReleaseEmptiedSeats()
		{
			_seated.Clear();
			for (int i = 0; i < _seatViews.Length; i++)
			{
				LobbyCharacterView lobbyCharacterView = _seatViews[i];
				if (!(lobbyCharacterView == null))
				{
					if (IsLive(lobbyCharacterView.Pirate))
					{
						_seated.Add(lobbyCharacterView.Pirate);
						continue;
					}
					Object.Destroy(lobbyCharacterView.gameObject);
					_seatViews[i] = null;
				}
			}
		}

		private void SeatNewPlayers()
		{
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (IsLive(piratePlayer) && !_seated.Contains(piratePlayer))
				{
					int num = FirstFreeSeat();
					if (num < 0)
					{
						break;
					}
					Transform transform = _seatPoints[num];
					LobbyCharacterView lobbyCharacterView = Object.Instantiate(_characterPrefab, transform.position, transform.rotation, transform);
					lobbyCharacterView.Bind(piratePlayer, _cosmetics);
					_seatViews[num] = lobbyCharacterView;
					_seated.Add(piratePlayer);
				}
			}
		}

		private static bool IsLive(PiratePlayer pirate)
		{
			if (pirate != null)
			{
				return pirate.IsSpawned;
			}
			return false;
		}

		private int FirstFreeSeat()
		{
			for (int i = 0; i < _seatViews.Length; i++)
			{
				if (_seatViews[i] == null && _seatPoints[i] != null)
				{
					return i;
				}
			}
			return -1;
		}

		private void BuildFallbackSeatsIfNeeded()
		{
			if (_seatPoints == null || _seatPoints.Length == 0)
			{
				int num = Mathf.Max(0, _fallbackSeatCount);
				_seatPoints = new Transform[num];
				float num2 = (float)(-(num - 1)) * 0.5f * _fallbackSpacing;
				for (int i = 0; i < num; i++)
				{
					GameObject gameObject = new GameObject("seat_" + i);
					gameObject.transform.SetParent(base.transform, worldPositionStays: false);
					gameObject.transform.localPosition = new Vector3(num2 + (float)i * _fallbackSpacing, 0f, 0f);
					gameObject.transform.localRotation = Quaternion.Euler(0f, _fallbackFacing, 0f);
					_seatPoints[i] = gameObject.transform;
				}
			}
		}
	}
}
