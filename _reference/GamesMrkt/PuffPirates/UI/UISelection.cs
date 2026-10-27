using System;
using Cysharp.Threading.Tasks;
using GamesMrkt.Inputs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI
{
	public static class UISelection
	{
		private sealed class Unsubscriber : IDisposable
		{
			private Action _dispose;

			public Unsubscriber(Action dispose)
			{
				_dispose = dispose;
			}

			public void Dispose()
			{
				_dispose?.Invoke();
				_dispose = null;
			}
		}

		private const float NAVIGATE_MIN_SQUARED_MAGNITUDE = 0.25f;

		public static GameObject Current
		{
			get
			{
				if (!(EventSystem.current != null))
				{
					return null;
				}
				return EventSystem.current.currentSelectedGameObject;
			}
		}

		public static bool IsLost
		{
			get
			{
				GameObject current = Current;
				if (!(current == null))
				{
					return !current.activeInHierarchy;
				}
				return true;
			}
		}

		public static void Select(GameObject target)
		{
			EventSystem current = EventSystem.current;
			if (!(current == null) && !(target == null) && target.activeInHierarchy && !current.alreadySelecting && (!target.TryGetComponent<Selectable>(out var component) || component.IsInteractable()))
			{
				current.SetSelectedGameObject(target);
			}
		}

		public static void Select(Selectable target)
		{
			if (target != null)
			{
				Select(target.gameObject);
			}
		}

		public static void SelectFirst(GameObject root)
		{
			if (root == null)
			{
				return;
			}
			Selectable[] componentsInChildren = root.GetComponentsInChildren<Selectable>();
			foreach (Selectable selectable in componentsInChildren)
			{
				if (selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None)
				{
					Select(selectable.gameObject);
					break;
				}
			}
		}

		public static IDisposable SubscribeNavigatePressed(InputPlayer player, Action handler)
		{
			if (player == null || handler == null)
			{
				return null;
			}
			bool isActive = true;
			IDisposable subscription = player.Subscribe("UI/Navigate", InputActionTrigger.Performed, (InputAction.CallbackContext context) =>
			{
				if (!(context.ReadValue<Vector2>().sqrMagnitude < 0.25f))
				{
					UniTask.Post(() =>
					{
						if (isActive)
						{
							handler();
						}
					}, PlayerLoopTiming.LastPostLateUpdate);
				}
			});
			return new Unsubscriber(() =>
			{
				isActive = false;
				subscription.Dispose();
			});
		}

		public static void LinkPair(Selectable first, Selectable second)
		{
			if (!(first == null) && !(second == null))
			{
				first.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnLeft = second,
					selectOnRight = second
				};
				second.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnLeft = first,
					selectOnRight = first
				};
			}
		}

		public static void Clear()
		{
			EventSystem current = EventSystem.current;
			if (current != null && !current.alreadySelecting)
			{
				current.SetSelectedGameObject(null);
			}
		}
	}
}
