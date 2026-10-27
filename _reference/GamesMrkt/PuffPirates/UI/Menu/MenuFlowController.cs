using System;
using System.Collections.Generic;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using PuffPirates.Settings;
using PuffPirates.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	public class MenuFlowController : MonoBehaviour
	{
		[SerializeField]
		private GameObject _menuRoot;

		[Tooltip("Optional. Plays the menu's outro before a screen that hides the menu root opens.")]
		[SerializeField]
		private MenuIntroAnimator _menuAnimator;

		[Tooltip("Optional. Main menu button selected at start, so arrows / gamepad work right away. Empty = the first button.")]
		[SerializeField]
		private Selectable _menuFirstSelected;

		private readonly Stack<MenuPanel> _stack = new Stack<MenuPanel>();

		private readonly Stack<GameObject> _selectionsBeforeOpen = new Stack<GameObject>();

		private readonly Dictionary<MenuPanel, Action> _closeHandlers = new Dictionary<MenuPanel, Action>();

		private bool _transitioning;

		private IDisposable _cancelSubscription;

		private IDisposable _navigateSubscription;

		private void Start()
		{
			SelectMenuDefault();
		}

		private void Awake()
		{
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
		}

		private void ResolveSystems()
		{
			if (!(this == null) && this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out var system))
			{
				_cancelSubscription = system.Player?.Subscribe("UI/Cancel", InputActionTrigger.Performed, HandleCancelPressed);
				_navigateSubscription = UISelection.SubscribeNavigatePressed(system.Player, HandleNavigatePressed);
			}
		}

		private void HandleNavigatePressed()
		{
			if (!_transitioning)
			{
				if (_stack.Count > 0)
				{
					_stack.Peek().RestoreSelectionIfLost();
				}
				else if (UISelection.IsLost)
				{
					SelectMenuDefault();
				}
			}
		}

		private void SelectMenuDefault()
		{
			if (_menuFirstSelected != null)
			{
				UISelection.Select(_menuFirstSelected);
			}
			else
			{
				UISelection.SelectFirst(_menuRoot);
			}
		}

		private void HandleCancelPressed()
		{
			if (!_transitioning && _stack.Count != 0 && !KeyBindings.IsRebinding)
			{
				MenuPanel menuPanel = _stack.Peek();
				if (menuPanel.CanCloseWithCancel)
				{
					AudioManager.PlaySound(CommonSound.ButtonClick);
					Close(menuPanel);
				}
			}
		}

		public void Open(MenuPanel panel)
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (_transitioning)
			{
				return;
			}
			bool flag = _menuRoot != null && _menuRoot.activeSelf;
			if ((panel.HidesMenuRoot & flag) && _menuAnimator != null)
			{
				_transitioning = true;
				_menuAnimator.PlayOutro(() =>
				{
					_transitioning = false;
					OpenNow(panel);
				});
			}
			else
			{
				OpenNow(panel);
			}
		}

		public void Close(MenuPanel panel)
		{
			if (_transitioning)
			{
				return;
			}
			if (_stack.Count == 0 || _stack.Peek() != panel)
			{
				Debug.LogWarning("MenuFlowController: tried to close " + panel.name + " but it is not the top panel");
				return;
			}
			_stack.Pop();
			UnsubscribeClose(panel);
			GameObject selectionBeforeOpen = ((_selectionsBeforeOpen.Count > 0) ? _selectionsBeforeOpen.Pop() : null);
			_transitioning = true;
			panel.Close(() =>
			{
				_transitioning = false;
				UpdateMenuRootVisibility();
				UISelection.Select(selectionBeforeOpen);
				Debug.Log($"MenuFlowController: closed {panel.name}, stack depth {_stack.Count}");
			});
		}

		private void OpenNow(MenuPanel panel)
		{
			_selectionsBeforeOpen.Push(UISelection.Current);
			_stack.Push(panel);
			SubscribeClose(panel);
			panel.Open();
			UpdateMenuRootVisibility();
			Debug.Log($"MenuFlowController: opened {panel.name}, stack depth {_stack.Count}");
		}

		private void SubscribeClose(MenuPanel panel)
		{
			if (!_closeHandlers.ContainsKey(panel))
			{
				Action value = () =>
				{
					Close(panel);
				};
				_closeHandlers[panel] = value;
				panel.Closed += value;
			}
		}

		private void UnsubscribeClose(MenuPanel panel)
		{
			if (_closeHandlers.Remove(panel, out var value))
			{
				panel.Closed -= value;
			}
		}

		private void OnDestroy()
		{
			foreach (KeyValuePair<MenuPanel, Action> closeHandler in _closeHandlers)
			{
				if (closeHandler.Key != null)
				{
					closeHandler.Key.Closed -= closeHandler.Value;
				}
			}
			_closeHandlers.Clear();
			_cancelSubscription?.Dispose();
			_navigateSubscription?.Dispose();
		}

		private void UpdateMenuRootVisibility()
		{
			bool flag = false;
			foreach (MenuPanel item in _stack)
			{
				if (item.HidesMenuRoot)
				{
					flag = true;
					break;
				}
			}
			if (_menuRoot != null)
			{
				_menuRoot.SetActive(!flag);
			}
		}
	}
}
