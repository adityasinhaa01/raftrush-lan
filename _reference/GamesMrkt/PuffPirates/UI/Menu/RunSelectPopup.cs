using System;
using System.Collections.Generic;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.Localization;
using PuffPirates.Save;
using PuffPirates.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	[RequireComponent(typeof(UIPopupAnimator))]
	public class RunSelectPopup : MonoBehaviour
	{
		[SerializeField]
		private UIPopupAnimator _animator;

		[Tooltip("Optional. Clicking outside the popup goes back one step, like ESC (confirm → details → list → closed).")]
		[SerializeField]
		private Button _backgroundButton;

		[Header("Run list")]
		[SerializeField]
		private GameObject _listRoot;

		[SerializeField]
		private Button _newGameButton;

		[SerializeField]
		private Transform _itemContainer;

		[SerializeField]
		private RunSlotItem _itemPrefab;

		[Header("Run details")]
		[SerializeField]
		private UIPopupAnimator _detailsAnimator;

		[SerializeField]
		private RunSlotItem _detailsItem;

		[SerializeField]
		private Button _loadButton;

		[SerializeField]
		private Button _deleteButton;

		[SerializeField]
		private Button _detailsCloseButton;

		[SerializeField]
		private ConfirmPopup _deleteConfirmPopup;

		private readonly List<RunSlotItem> _items = new List<RunSlotItem>();

		private HostRunSystem _runs;

		private NotificationSystem _notifications;

		private Action _onRunChosen;

		private string _selectedRunId;

		private IDisposable _cancelSubscription;

		private IDisposable _navigateSubscription;

		private int _hiddenFrame = -1;

		private GameObject _selectionBeforeShow;

		private RunSlotItem _detailsSourceItem;

		public bool IsOpen
		{
			get
			{
				if (_animator != null)
				{
					return _animator.IsOpen;
				}
				return false;
			}
		}

		public bool IsTakingCancel
		{
			get
			{
				if (!IsOpen)
				{
					return Time.frameCount == _hiddenFrame;
				}
				return true;
			}
		}

		private bool IsDetailsOpen
		{
			get
			{
				if (_detailsAnimator != null)
				{
					return _detailsAnimator.IsOpen;
				}
				return false;
			}
		}

		private void Awake()
		{
			if (_backgroundButton != null)
			{
				_backgroundButton.onClick.AddListener(HandleBackgroundClicked);
			}
			_newGameButton.onClick.AddListener(HandleNewGameClicked);
			_loadButton.onClick.AddListener(HandleLoadClicked);
			_deleteButton.onClick.AddListener(HandleDeleteClicked);
			_detailsCloseButton.onClick.AddListener(HandleDetailsCloseClicked);
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
		}

		private void OnDestroy()
		{
			if (_backgroundButton != null)
			{
				_backgroundButton.onClick.RemoveListener(HandleBackgroundClicked);
			}
			if (_newGameButton != null)
			{
				_newGameButton.onClick.RemoveListener(HandleNewGameClicked);
			}
			if (_loadButton != null)
			{
				_loadButton.onClick.RemoveListener(HandleLoadClicked);
			}
			if (_deleteButton != null)
			{
				_deleteButton.onClick.RemoveListener(HandleDeleteClicked);
			}
			if (_detailsCloseButton != null)
			{
				_detailsCloseButton.onClick.RemoveListener(HandleDetailsCloseClicked);
			}
			if (_runs != null)
			{
				_runs.OnRunsChanged -= HandleRunsChanged;
			}
			_cancelSubscription?.Dispose();
			_navigateSubscription?.Dispose();
		}

		private void HandleCancelPressed()
		{
			if (base.isActiveAndEnabled && IsOpen)
			{
				GoBack();
			}
		}

		private void HandleNavigatePressed()
		{
			if (base.isActiveAndEnabled && IsOpen && (!(_deleteConfirmPopup != null) || !_deleteConfirmPopup.IsOpen))
			{
				Transform parent = (IsDetailsOpen ? _detailsAnimator.transform : base.transform);
				GameObject current = UISelection.Current;
				if (!(current != null) || !current.activeInHierarchy || !current.transform.IsChildOf(parent))
				{
					UISelection.Select(IsDetailsOpen ? _loadButton : _newGameButton);
				}
			}
		}

		private void ResolveSystems()
		{
			if (this == null)
			{
				return;
			}
			this.TryGetSystem<NotificationSystem, CoreSystemHandler>(out _notifications);
			if (this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out var system))
			{
				_cancelSubscription = system.Player?.Subscribe("UI/Cancel", InputActionTrigger.Performed, HandleCancelPressed);
				_navigateSubscription = UISelection.SubscribeNavigatePressed(system.Player, HandleNavigatePressed);
			}
			if (this.TryGetSystem<HostRunSystem, CoreSystemHandler>(out _runs))
			{
				_runs.OnRunsChanged += HandleRunsChanged;
				if (IsOpen)
				{
					RebuildList();
				}
			}
		}

		public void Show(Action onRunChosen)
		{
			if (TryGetRuns(out var runSystem) && runSystem.Runs.Count == 0)
			{
				runSystem.StartNew();
				onRunChosen?.Invoke();
				return;
			}
			_onRunChosen = onRunChosen;
			CloseDetails(instant: true);
			if (_deleteConfirmPopup != null)
			{
				_deleteConfirmPopup.Hide(instant: true);
			}
			RebuildList();
			if (!IsOpen)
			{
				_selectionBeforeShow = UISelection.Current;
			}
			_animator.Show();
			UISelection.Select(_newGameButton);
		}

		public void Hide(bool instant = false)
		{
			_onRunChosen = null;
			bool isOpen = IsOpen;
			if (isOpen)
			{
				_hiddenFrame = Time.frameCount;
			}
			if (_deleteConfirmPopup != null)
			{
				_deleteConfirmPopup.Hide(instant);
			}
			CloseDetails(instant);
			if (_animator != null)
			{
				_animator.Hide(instant);
			}
			if (isOpen)
			{
				UISelection.Select(_selectionBeforeShow);
			}
			_selectionBeforeShow = null;
		}

		private bool TryGetRuns(out HostRunSystem runSystem)
		{
			if (_runs == null && Singleton<CoreSystemHandler>.Instance != null)
			{
				this.TryGetSystem<HostRunSystem, CoreSystemHandler>(out _runs);
			}
			runSystem = _runs;
			return runSystem != null;
		}

		private void RebuildList()
		{
			IReadOnlyList<HostRunData> readOnlyList2;
			if (_runs == null)
			{
				IReadOnlyList<HostRunData> readOnlyList = Array.Empty<HostRunData>();
				readOnlyList2 = readOnlyList;
			}
			else
			{
				readOnlyList2 = _runs.Runs;
			}
			IReadOnlyList<HostRunData> readOnlyList3 = readOnlyList2;
			while (_items.Count < readOnlyList3.Count)
			{
				_items.Add(CreateItem());
			}
			for (int i = 0; i < _items.Count; i++)
			{
				bool flag = i < readOnlyList3.Count;
				_items[i].gameObject.SetActive(flag);
				if (flag)
				{
					_items[i].Bind(readOnlyList3[i]);
				}
			}
			LinkListNavigation(readOnlyList3.Count);
		}

		private void LinkListNavigation(int usedItemCount)
		{
			Selectable selectable = _newGameButton;
			LinkVertical(_newGameButton, null, null);
			for (int i = 0; i < usedItemCount; i++)
			{
				Selectable selectButton = _items[i].SelectButton;
				if (!(selectButton == null))
				{
					LinkVertical(selectButton, selectable, null);
					LinkVertical(selectable, selectable.navigation.selectOnUp, selectButton);
					selectable = selectButton;
				}
			}
		}

		private static void LinkVertical(Selectable selectable, Selectable up, Selectable down)
		{
			selectable.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnUp = up,
				selectOnDown = down
			};
		}

		private RunSlotItem CreateItem()
		{
			RunSlotItem runSlotItem = UnityEngine.Object.Instantiate(_itemPrefab, _itemContainer);
			runSlotItem.OnSelected += HandleItemSelected;
			return runSlotItem;
		}

		private void OpenDetails(HostRunData run)
		{
			_selectedRunId = run.id;
			_detailsItem.Bind(run);
			_listRoot.SetActive(value: false);
			_detailsAnimator.Show();
			UISelection.Select(_loadButton);
		}

		private void CloseDetails(bool instant = false)
		{
			string selectedRunId = _selectedRunId;
			_selectedRunId = null;
			if (_detailsAnimator != null)
			{
				_detailsAnimator.Hide(instant);
			}
			if (_listRoot != null)
			{
				_listRoot.SetActive(value: true);
			}
			if (_detailsSourceItem != null && _detailsSourceItem.gameObject.activeInHierarchy && _detailsSourceItem.RunId == selectedRunId)
			{
				UISelection.SelectFirst(_detailsSourceItem.gameObject);
			}
			else
			{
				UISelection.Select(_newGameButton);
			}
			_detailsSourceItem = null;
		}

		private void HandleItemSelected(RunSlotItem item)
		{
			HostRunData hostRunData = FindRun(item.RunId);
			if (hostRunData != null)
			{
				_detailsSourceItem = item;
				OpenDetails(hostRunData);
			}
		}

		private void HandleNewGameClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (_runs != null)
			{
				if (!_runs.CanStartNew)
				{
					_notifications?.Show(Loc.Get("runs.limit_reached"), isGoodAlert: false);
					return;
				}
				_runs.StartNew();
				ChooseRun();
			}
		}

		private void HandleLoadClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (_runs != null && _runs.Continue(_selectedRunId))
			{
				ChooseRun();
			}
		}

		private void HandleDeleteClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (_runs != null && _selectedRunId != null)
			{
				string runId = _selectedRunId;
				_detailsAnimator.Hide();
				_deleteConfirmPopup.Show("runs.delete_confirm.title", "runs.delete_confirm.desc", "runs.delete_confirm.yes", "runs.delete_confirm.no", () =>
				{
					DeleteRun(runId);
				}, HandleDeleteCancelled);
			}
		}

		private void HandleDeleteCancelled()
		{
			_detailsAnimator.Show();
			UISelection.Select(_deleteButton);
		}

		private void DeleteRun(string runId)
		{
			_runs.Delete(runId);
			CloseDetails();
		}

		private void GoBack()
		{
			if (_deleteConfirmPopup != null && _deleteConfirmPopup.IsOpen)
			{
				_deleteConfirmPopup.Cancel();
			}
			else if (IsDetailsOpen)
			{
				CloseDetails();
			}
			else
			{
				Hide();
			}
		}

		private void HandleDetailsCloseClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			CloseDetails();
		}

		private void HandleBackgroundClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			GoBack();
		}

		private void HandleRunsChanged()
		{
			if (IsOpen)
			{
				RebuildList();
			}
		}

		private HostRunData FindRun(string runId)
		{
			if (_runs == null)
			{
				return null;
			}
			foreach (HostRunData run in _runs.Runs)
			{
				if (run.id == runId)
				{
					return run;
				}
			}
			return null;
		}

		private void ChooseRun()
		{
			Action onRunChosen = _onRunChosen;
			Hide();
			onRunChosen?.Invoke();
		}
	}
}
