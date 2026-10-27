using System;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Localization;
using PuffPirates.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI
{
	[RequireComponent(typeof(UIPopupAnimator))]
	public class ConfirmPopup : MonoBehaviour
	{
		[SerializeField]
		private UIPopupAnimator _animator;

		[Header("Labels")]
		[SerializeField]
		private LocalizedText _title;

		[SerializeField]
		private LocalizedText _description;

		[SerializeField]
		private LocalizedText _confirmLabel;

		[SerializeField]
		private LocalizedText _cancelLabel;

		[Header("Buttons")]
		[SerializeField]
		private Button _confirmButton;

		[SerializeField]
		private Button _cancelButton;

		private Action _onConfirm;

		private Action _onCancel;

		private GameObject _selectionBeforeShow;

		private IDisposable _navigateSubscription;

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

		private void Awake()
		{
			_confirmButton.onClick.AddListener(HandleConfirmClicked);
			_cancelButton.onClick.AddListener(HandleCancelClicked);
			UISelection.LinkPair(_confirmButton, _cancelButton);
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
		}

		private void ResolveSystems()
		{
			if (!(this == null) && this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out var system))
			{
				_navigateSubscription = UISelection.SubscribeNavigatePressed(system.Player, HandleNavigatePressed);
			}
		}

		private void HandleNavigatePressed()
		{
			if (IsOpen)
			{
				GameObject current = UISelection.Current;
				if (!(current != null) || !current.activeInHierarchy || !current.transform.IsChildOf(base.transform))
				{
					UISelection.Select(_cancelButton);
				}
			}
		}

		private void OnDestroy()
		{
			if (_confirmButton != null)
			{
				_confirmButton.onClick.RemoveListener(HandleConfirmClicked);
			}
			if (_cancelButton != null)
			{
				_cancelButton.onClick.RemoveListener(HandleCancelClicked);
			}
			_navigateSubscription?.Dispose();
		}

		public void Show(string titleKey, string descriptionKey, string confirmKey, string cancelKey, Action onConfirm, Action onCancel = null)
		{
			_onConfirm = onConfirm;
			_onCancel = onCancel;
			SetKey(_title, titleKey);
			SetKey(_description, descriptionKey);
			SetKey(_confirmLabel, confirmKey);
			SetKey(_cancelLabel, cancelKey);
			if (!IsOpen)
			{
				_selectionBeforeShow = UISelection.Current;
			}
			_animator.Show();
			UISelection.Select(_cancelButton);
		}

		public void Hide(bool instant = false)
		{
			_onConfirm = null;
			_onCancel = null;
			bool isOpen = IsOpen;
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

		public void Cancel()
		{
			if (IsOpen)
			{
				Action onCancel = _onCancel;
				Hide();
				onCancel?.Invoke();
			}
		}

		private void HandleConfirmClicked()
		{
			if (IsOpen)
			{
				AudioManager.PlaySound(CommonSound.ButtonClick);
				Action onConfirm = _onConfirm;
				Hide();
				onConfirm?.Invoke();
			}
		}

		private void HandleCancelClicked()
		{
			if (IsOpen)
			{
				AudioManager.PlaySound(CommonSound.ButtonClick);
				Cancel();
			}
		}

		private static void SetKey(LocalizedText label, string key)
		{
			if (label != null && !string.IsNullOrEmpty(key))
			{
				label.SetKey(key);
			}
		}
	}
}
