using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates;
using GamesMrkt.Scene;
using PuffPirates.Systems;
using UnityEngine;

namespace PuffPirates.Networking
{
	public class Bootstrap : MonoBehaviour
	{
		private void Start()
		{
			Singleton<CoreSystemHandler>.Instance.WhenReady(LoadMenuScene);
		}

		private void LoadMenuScene()
		{
			LoadMenuSceneAsync().Forget();
		}

		private async UniTaskVoid LoadMenuSceneAsync()
		{
			this.TryGetSystem<LoadingSystem, CoreSystemHandler>(out var loading);
			SceneFlowSystem sceneFlow = this.GetSystem<SceneFlowSystem, CoreSystemHandler>();
			if (loading != null)
			{
				await loading.ShowAsync();
			}
			await sceneFlow.LoadAsync("MenuScene");
			if (loading != null)
			{
				await loading.HideAsync("MenuScene");
			}
		}
	}
}
