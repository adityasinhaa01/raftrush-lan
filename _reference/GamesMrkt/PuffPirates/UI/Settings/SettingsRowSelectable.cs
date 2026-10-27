using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class SettingsRowSelectable : Selectable, ISubmitHandler, IEventSystemHandler, ICancelHandler
	{
		private const float COLOR_FADE_DURATION = 0.1f;

		[Header("Background")]
		[SerializeField]
		private Graphic _background;

		[SerializeField]
		private Color _selectedBackgroundColor = new Color(208f / 255f, 70f / 255f, 129f / 255f, 1f);

		[Tooltip("While a slider / option row is being edited (left / right change the value).")]
		[SerializeField]
		private Color _editingBackgroundColor = new Color(148f / 255f, 36f / 255f, 86f / 255f, 1f);

		[Header("Title")]
		[SerializeField]
		private TMP_Text _title;

		[SerializeField]
		private Color _selectedTitleColor = Color.white;

		[SerializeField]
		private Color _editingTitleColor = Color.white;

		private SettingsRowBase _row;

		private Color _normalBackgroundColor;

		private Color _normalTitleColor;

		public bool IsEditing { get; private set; }

		public event Action<int> OnTabStepRequested;

		public event Action<bool> OnEditingChanged;

		protected override void Awake()
		{
			base.Awake();
			base.transition = Transition.None;
			_row = GetComponent<SettingsRowBase>();
			if (_background != null)
			{
				_normalBackgroundColor = _background.color;
			}
			if (_title != null)
			{
				_normalTitleColor = _title.color;
			}
		}

		public void LinkVertical(Selectable up, Selectable down)
		{
			base.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnUp = up,
				selectOnDown = down
			};
		}

		public void SetEditing(bool isEditing)
		{
			if (IsEditing != isEditing)
			{
				IsEditing = isEditing;
				DoStateTransition(base.currentSelectionState, instant: false);
				OnEditingChanged?.Invoke(isEditing);
			}
		}

		public override void OnMove(AxisEventData eventData)
		{
			if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)
			{
				int num = ((eventData.moveDir == MoveDirection.Right) ? 1 : (-1));
				if (IsEditing)
				{
					_row.Step(num);
				}
				else
				{
					OnTabStepRequested?.Invoke(num);
				}
				eventData.Use();
			}
			else if (IsEditing)
			{
				eventData.Use();
			}
			else
			{
				base.OnMove(eventData);
			}
		}

		public void OnSubmit(BaseEventData eventData)
		{
			if (!(_row == null) && IsInteractable())
			{
				if (IsEditing)
				{
					SetEditing(isEditing: false);
				}
				else if (_row.CanEdit)
				{
					SetEditing(isEditing: true);
				}
				else
				{
					_row.Submit();
				}
			}
		}

		public void OnCancel(BaseEventData eventData)
		{
			if (IsEditing)
			{
				SetEditing(isEditing: false);
			}
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			base.OnDeselect(eventData);
			SetEditing(isEditing: false);
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			if (eventData != null && eventData.delta.sqrMagnitude > 0f && Cursor.visible && IsInteractable())
			{
				EventSystem current = EventSystem.current;
				if (current != null && !current.alreadySelecting)
				{
					current.SetSelectedGameObject(base.gameObject, eventData);
				}
			}
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (Application.isPlaying)
			{
				bool flag = state == SelectionState.Selected || state == SelectionState.Pressed;
				if (IsEditing)
				{
					ApplyColors(_editingBackgroundColor, _editingTitleColor, instant);
				}
				else if (flag)
				{
					ApplyColors(_selectedBackgroundColor, _selectedTitleColor, instant);
				}
				else
				{
					ApplyColors(_normalBackgroundColor, _normalTitleColor, instant);
				}
			}
		}

		private void ApplyColors(Color backgroundColor, Color titleColor, bool instant)
		{
			ApplyColor(_background, backgroundColor, instant);
			ApplyColor(_title, titleColor, instant);
		}

		private void ApplyColor(Graphic graphic, Color color, bool instant)
		{
			if (!(graphic == null))
			{
				graphic.DOKill();
				if (instant || !base.isActiveAndEnabled)
				{
					graphic.color = color;
				}
				else
				{
					graphic.DOColor(color, 0.1f).SetUpdate(isIndependentUpdate: true).SetLink(graphic.gameObject);
				}
			}
		}
	}
}
