using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates;
using PuffPirates.Networking;
using PuffPirates.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuffPirates.Audio
{
	public class MusicController : MonoBehaviour
	{
		[Tooltip("Fade in / fade out / crossfade duration (s).")]
		[SerializeField]
		private float _fadeDuration = 1f;

		[Tooltip("Menu music volume in the lobby, relative to the player's music volume setting.")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _lobbyVolume = 0.5f;

		[Tooltip("Gameplay music starts as soon as this scene is active, before the countdown ends.")]
		[SerializeField]
		private string _gameplayScene = "PuffTest";

		[Tooltip("2D sound played once when the race finishes (every peer reaches Finished from the synced race state). Empty = silent.")]
		[SerializeField]
		private SoundData _gameFinishedSound;

		private ConnectionSystem _connection;

		private int _gen;

		private void Start()
		{
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
			if (_gameFinishedSound != null)
			{
				AudioManager.PreloadAsync(_gameFinishedSound).Forget();
			}
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				this.WhenSystemReady<ConnectionSystem, CoreSystemHandler>(BindConnection);
			}
		}

		private void BindConnection(ConnectionSystem connection)
		{
			if (!(this == null) && !(_connection != null))
			{
				_connection = connection;
				_connection.StateChanged += OnStateChanged;
				ApplyMusic();
			}
		}

		private void OnDestroy()
		{
			_gen++;
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			if (_connection != null)
			{
				_connection.StateChanged -= OnStateChanged;
			}
		}

		private void OnStateChanged(LauncherState previous, LauncherState next)
		{
			if (next == LauncherState.Finished && previous != next && _gameFinishedSound != null)
			{
				AudioManager.PlaySound(_gameFinishedSound);
			}
			ApplyMusic();
		}

		private void OnActiveSceneChanged(Scene previous, Scene next)
		{
			ApplyMusic();
		}

		private async void ApplyMusic()
		{
			if (_connection == null)
			{
				return;
			}
			int gen = ++_gen;
			await AudioManager.PreloadLibraryAsync();
			if (gen != _gen || this == null)
			{
				return;
			}
			LauncherState state = _connection.State;
			int num;
			switch (state)
			{
			case LauncherState.Finished:
				AudioManager.StopMusic(_fadeDuration);
				return;
			default:
				num = ((SceneManager.GetActiveScene().name == _gameplayScene) ? 1 : 0);
				break;
			case LauncherState.Racing:
				num = 1;
				break;
			}
			if (num != 0)
			{
				Play(CommonMusic.Gameplay, 1f);
				return;
			}
			switch (state)
			{
			case LauncherState.Lobby:
				Play(CommonMusic.MainMenu, _lobbyVolume);
				break;
			case LauncherState.Menu:
				Play(CommonMusic.MainMenu, 1f);
				break;
			default:
				Play(CommonMusic.MainMenu, null);
				break;
			}
		}

		private void Play(CommonMusic music, float? volume)
		{
			MusicData musicData = ((AudioManager.Library != null) ? AudioManager.Library.Get(music) : null);
			if (musicData == null)
			{
				return;
			}
			if (AudioManager.CurrentMusic == musicData)
			{
				if (volume.HasValue)
				{
					AudioManager.FadeMusicVolume(volume.Value, _fadeDuration);
				}
				return;
			}
			AudioManager.PlayMusic(musicData, _fadeDuration);
			if (!(AudioManager.CurrentMusic != musicData))
			{
				AudioManager.FadeMusicVolume(0f, 0f);
				AudioManager.FadeMusicVolume(volume ?? 1f, _fadeDuration);
			}
		}
	}
}
