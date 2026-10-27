using System;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates;
using GamesMrkt.PuffPirates.UI;
using GamesMrkt.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuffPirates.Systems
{
	public class LoadingSystem : MonoBehaviour, IGameSystem
	{
		[SerializeField]
		private LoadingCoverView _view;

		private static readonly TimeSpan MinDisplayDuration = TimeSpan.FromSeconds(0.6);

		private float _shownAtRealtime = -1f;

		public string SystemId => "LoadingSystem";

		public UniTask Initialize()
		{
			UnityEngine.Object.DontDestroyOnLoad(_view.gameObject);
			return UniTask.CompletedTask;
		}

		public UniTask ShowAsync()
		{
			if (_view != null)
			{
				_view.Show();
			}
			if (_shownAtRealtime < 0f)
			{
				_shownAtRealtime = Time.realtimeSinceStartup;
			}
			return UniTask.CompletedTask;
		}

		public async UniTask HideAsync(string preloadScene = null)
		{
			if (!string.IsNullOrEmpty(preloadScene) && Singleton<CoreSystemHandler>.Instance != null)
			{
				PreloadSystem system = Singleton<CoreSystemHandler>.Instance.GetSystem<PreloadSystem>();
				Scene sceneByName = SceneManager.GetSceneByName(preloadScene);
				if (system != null && sceneByName.IsValid() && sceneByName.isLoaded)
				{
					await system.PreloadSceneAsync(sceneByName);
				}
			}
			if (_shownAtRealtime >= 0f)
			{
				float num = Time.realtimeSinceStartup - _shownAtRealtime;
				float num2 = (float)MinDisplayDuration.TotalSeconds - num;
				if (num2 > 0f)
				{
					await UniTask.Delay(TimeSpan.FromSeconds(num2), ignoreTimeScale: true);
				}
				_shownAtRealtime = -1f;
			}
			if (_view != null)
			{
				_view.Hide();
			}
		}
	}
}
