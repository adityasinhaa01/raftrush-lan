using GamesMrkt.Core;
using PuffPirates.Systems;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Gameplay
{
	public class HostOnlyVisibility : MonoBehaviour
	{
		[Tooltip("Object shown only on the host. Empty = this GameObject.")]
		[SerializeField]
		private GameObject _target;

		private ConnectionSystem _connection;

		private CanvasGroup _group;

		private bool _shown = true;

		private void Awake()
		{
			GameObject gameObject = ((_target != null) ? _target : base.gameObject);
			if (!gameObject.TryGetComponent<CanvasGroup>(out _group))
			{
				_group = gameObject.AddComponent<CanvasGroup>();
			}
			SetShown(show: false);
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
		}

		private void ResolveSystems()
		{
			if (!(this == null))
			{
				this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out _connection);
			}
		}

		private void Update()
		{
			SetShown(_connection != null && _connection.IsHost);
		}

		private void SetShown(bool show)
		{
			if (!(_group == null) && _shown != show)
			{
				_shown = show;
				_group.alpha = (show ? 1f : 0f);
				_group.blocksRaycasts = show;
				_group.interactable = show;
			}
		}
	}
}
