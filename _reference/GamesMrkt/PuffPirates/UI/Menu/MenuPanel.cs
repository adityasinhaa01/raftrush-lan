using System;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	public abstract class MenuPanel : MonoBehaviour
	{
		[SerializeField]
		private bool _hidesMenuRoot;

		[SerializeField]
		private GameObject _root;

		[Tooltip("Optional. When set, open/close are animated through it instead of a plain SetActive; it should sit on the root object.")]
		[SerializeField]
		private UIPopupAnimator _animator;

		[Tooltip("Optional. Selected when the screen opens, so arrows / gamepad start from it. Empty = its first button.")]
		[SerializeField]
		private Selectable _firstSelected;

		public bool HidesMenuRoot => _hidesMenuRoot;

		public virtual bool CanCloseWithCancel => true;

		private GameObject Root
		{
			get
			{
				if (!(_root != null))
				{
					return base.gameObject;
				}
				return _root;
			}
		}

		public event Action Closed;

		protected void RequestClose()
		{
			Closed?.Invoke();
		}

		public virtual void Open()
		{
			if (_animator != null)
			{
				_animator.Show();
			}
			else
			{
				Root.SetActive(value: true);
			}
			SelectDefault();
		}

		public void RestoreSelectionIfLost()
		{
			if (!HasUsableSelection())
			{
				SelectDefault();
			}
		}

		protected virtual bool HasUsableSelection()
		{
			return !UISelection.IsLost;
		}

		protected virtual void SelectDefault()
		{
			if (_firstSelected != null)
			{
				UISelection.Select(_firstSelected);
			}
			else
			{
				UISelection.SelectFirst(Root);
			}
		}

		public virtual void Close(Action onClosed = null)
		{
			if (_animator != null)
			{
				_animator.Hide(instant: false, onClosed);
				return;
			}
			Root.SetActive(value: false);
			onClosed?.Invoke();
		}
	}
}
