using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.Localization;
using GamesMrkt.PuffPirates.UI.Menu;
using GamesMrkt.Resource;
using PuffPirates.CameraRig;
using PuffPirates.Settings;
using PuffPirates.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class SettingsPanelController : MenuPanel
	{
		private struct KeybindRowEntry
		{
			public GameAction Action;

			public string ControlScheme;

			public KeybindSettingsRow Row;
		}

		[SerializeField]
		private Button _backButton;

		[Header("Sabit metinler (satır değil — prefab'da duran yazılar)")]
		[Tooltip("Ekranın üst başlığı. Boş bırakılırsa prefab'daki yazı olduğu gibi kalır.")]
		[SerializeField]
		private TMP_Text _titleLabel;

		[Header("Footer (Ref/8-11.jpg — satır değil, altta duran iki pill)")]
		[SerializeField]
		private Button _privacyButton;

		[SerializeField]
		private TMP_Text _privacyLabel;

		[SerializeField]
		private Button _termsButton;

		[SerializeField]
		private TMP_Text _termsLabel;

		[SerializeField]
		private SettingsRowFactory _factory;

		[SerializeField]
		private Transform _content;

		[SerializeField]
		private ScrollRect _scrollRect;

		[SerializeField]
		private List<SettingsTabButton> _tabs;

		[SerializeField]
		private SettingsTabType _defaultTab;

		private readonly List<SettingsRowBase> _rows = new List<SettingsRowBase>();

		private readonly List<KeybindRowEntry> _keybindRows = new List<KeybindRowEntry>();

		private readonly List<IDisposable> _inputSubscriptions = new List<IDisposable>();

		private SettingsTabType _currentTab;

		private bool _initialized;

		private bool _isOpen;

		private bool _isRowEditing;

		private int _rowEditingEndedFrame = -1;

		private bool _rebuildQueued;

		public override bool CanCloseWithCancel
		{
			get
			{
				if (!_isRowEditing)
				{
					return Time.frameCount != _rowEditingEndedFrame;
				}
				return false;
			}
		}

		private bool CanReadInput
		{
			get
			{
				if (_initialized && _isOpen && !_isRowEditing)
				{
					return !KeyBindings.IsRebinding;
				}
				return false;
			}
		}

		public override void Open()
		{
			_isOpen = true;
			base.Open();
			if (!_initialized)
			{
				BuildRows();
				BuildTabs();
				SelectTab(_defaultTab);
				_initialized = true;
				SelectDefault();
			}
		}

		public override void Close(Action onClosed = null)
		{
			_isOpen = false;
			base.Close(onClosed);
		}

		protected override bool HasUsableSelection()
		{
			GameObject current = UISelection.Current;
			if (current == null || !current.activeInHierarchy)
			{
				return false;
			}
			if (!current.TryGetComponent<SettingsRowSelectable>(out var _))
			{
				return IsFooterButton(current);
			}
			return true;
		}

		private bool IsFooterButton(GameObject target)
		{
			if (!(_privacyButton != null) || !(target == _privacyButton.gameObject))
			{
				if (_termsButton != null)
				{
					return target == _termsButton.gameObject;
				}
				return false;
			}
			return true;
		}

		protected override void SelectDefault()
		{
			SettingsRowBase settingsRowBase = FindRowOf(UISelection.Current);
			Selectable selectable = ((settingsRowBase != null && settingsRowBase.gameObject.activeInHierarchy && settingsRowBase.RowSelectable != null) ? settingsRowBase.RowSelectable : FindFirstRowSelectable());
			if (selectable != null)
			{
				UISelection.Select(selectable);
			}
			else
			{
				base.SelectDefault();
			}
		}

		private SettingsRowBase FindRowOf(GameObject target)
		{
			int num = ((target != null) ? FindRowIndex(target) : (-1));
			if (num < 0)
			{
				return null;
			}
			return _rows[num];
		}

		private void Awake()
		{
			if (_backButton != null)
			{
				_backButton.onClick.AddListener(HandleBackClicked);
			}
			if (_privacyButton != null)
			{
				_privacyButton.onClick.AddListener(OpenPrivacyPolicy);
			}
			if (_termsButton != null)
			{
				_termsButton.onClick.AddListener(OpenTermsOfService);
			}
			Loc.OnLanguageChanged += HandleLanguageChanged;
			KeyBindings.OnChanged += RefreshKeybindRows;
			LanguageFollow.Label(_titleLabel);
			LanguageFollow.Label(_privacyLabel);
			LanguageFollow.Label(_termsLabel);
			ApplyStaticText();
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
		}

		private void ResolveSystems()
		{
			if (!(this == null) && this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out var system) && system.Player != null)
			{
				InputPlayer player = system.Player;
				_inputSubscriptions.Add(player.Subscribe("UI/PreviousTab", InputActionTrigger.Performed, HandlePreviousTabPressed));
				_inputSubscriptions.Add(player.Subscribe("UI/NextTab", InputActionTrigger.Performed, HandleNextTabPressed));
				_inputSubscriptions.Add(player.Subscribe("UI/Cancel", InputActionTrigger.Performed, HandleCancelPressed));
			}
		}

		private void HandlePreviousTabPressed()
		{
			if (CanReadInput)
			{
				StepTab(-1);
			}
		}

		private void HandleNextTabPressed()
		{
			if (CanReadInput)
			{
				StepTab(1);
			}
		}

		private void StepTab(int step)
		{
			int num = _tabs.FindIndex((SettingsTabButton tab) => tab.Type == _currentTab);
			if (num >= 0 && _tabs.Count != 0)
			{
				AudioManager.PlaySound(CommonSound.ButtonClick);
				num = (num + step + _tabs.Count) % _tabs.Count;
				SelectTab(_tabs[num].Type);
			}
		}

		private void HandleCancelPressed()
		{
			if (_initialized && KeyBindings.IsRebinding)
			{
				KeyBindings.CancelRebind();
			}
		}

		private void ApplyStaticText()
		{
			if (_titleLabel != null)
			{
				_titleLabel.text = Loc.Get("settings.title");
			}
			if (_privacyLabel != null)
			{
				_privacyLabel.text = Loc.Get("settings.footer.privacy");
			}
			if (_termsLabel != null)
			{
				_termsLabel.text = Loc.Get("settings.footer.terms");
			}
		}

		private void OpenPrivacyPolicy()
		{
			OpenResourceLink("pp_link");
		}

		private void OpenTermsOfService()
		{
			OpenResourceLink("tos_link");
		}

		private void OpenResourceLink(string resourceKey)
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (!this.TryGetSystem<ResourceSystem, CoreSystemHandler>(out var system) || system == null)
			{
				Debug.LogWarning("[SettingsPanelController] ResourceSystem yok — '" + resourceKey + "' açılamadı.");
				return;
			}
			string text = system.GetString(resourceKey);
			if (string.IsNullOrWhiteSpace(text))
			{
				Debug.LogWarning("[SettingsPanelController] '" + resourceKey + "' boş — bağlantı açılmadı.");
			}
			else
			{
				Application.OpenURL(text);
			}
		}

		private void OnDestroy()
		{
			if (_backButton != null)
			{
				_backButton.onClick.RemoveListener(HandleBackClicked);
			}
			if (_privacyButton != null)
			{
				_privacyButton.onClick.RemoveListener(OpenPrivacyPolicy);
			}
			if (_termsButton != null)
			{
				_termsButton.onClick.RemoveListener(OpenTermsOfService);
			}
			Loc.OnLanguageChanged -= HandleLanguageChanged;
			KeyBindings.OnChanged -= RefreshKeybindRows;
			foreach (SettingsTabButton tab in _tabs)
			{
				tab.OnSelected -= SelectTab;
			}
			foreach (IDisposable inputSubscription in _inputSubscriptions)
			{
				inputSubscription.Dispose();
			}
			_inputSubscriptions.Clear();
		}

		private void Update()
		{
			if (_rebuildQueued)
			{
				_rebuildQueued = false;
				ApplyStaticText();
				RebuildRows();
			}
		}

		private void HandleBackClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			RequestClose();
		}

		private void QueueRebuild()
		{
			_rebuildQueued = true;
		}

		private void HandleLanguageChanged(string language)
		{
			QueueRebuild();
		}

		private void BuildTabs()
		{
			foreach (SettingsTabButton tab in _tabs)
			{
				tab.OnSelected += SelectTab;
			}
		}

		private void SelectTab(SettingsTabType type)
		{
			_currentTab = type;
			foreach (SettingsRowBase row in _rows)
			{
				row.gameObject.SetActive(row.TabType == type);
			}
			foreach (SettingsTabButton tab in _tabs)
			{
				tab.SetSelected(tab.Type == type);
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(_content as RectTransform);
			if (_scrollRect != null)
			{
				_scrollRect.verticalNormalizedPosition = 1f;
			}
			LinkRowNavigation();
			GameObject current3 = UISelection.Current;
			if (!(current3 != null) || !current3.activeInHierarchy || !current3.TryGetComponent<SettingsRowSelectable>(out var _))
			{
				UISelection.Select(FindFirstRowSelectable());
			}
		}

		private void LinkRowNavigation()
		{
			SettingsRowSelectable settingsRowSelectable = null;
			foreach (SettingsRowBase row in _rows)
			{
				SettingsRowSelectable rowSelectable = row.RowSelectable;
				if (!(rowSelectable == null) && row.gameObject.activeSelf)
				{
					rowSelectable.LinkVertical(settingsRowSelectable, null);
					if (settingsRowSelectable != null)
					{
						settingsRowSelectable.LinkVertical(settingsRowSelectable.navigation.selectOnUp, rowSelectable);
					}
					settingsRowSelectable = rowSelectable;
				}
			}
			Selectable selectable = ((_privacyButton != null) ? _privacyButton : _termsButton);
			if (settingsRowSelectable != null && selectable != null)
			{
				settingsRowSelectable.LinkVertical(settingsRowSelectable.navigation.selectOnUp, selectable);
			}
			LinkFooterButton(_privacyButton, settingsRowSelectable, null, _termsButton);
			LinkFooterButton(_termsButton, settingsRowSelectable, _privacyButton, null);
		}

		private static void LinkFooterButton(Button button, Selectable up, Selectable left, Selectable right)
		{
			if (!(button == null))
			{
				button.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = up,
					selectOnLeft = left,
					selectOnRight = right
				};
			}
		}

		private Selectable FindFirstRowSelectable()
		{
			foreach (SettingsRowBase row in _rows)
			{
				if (row.gameObject.activeSelf && row.RowSelectable != null)
				{
					return row.RowSelectable;
				}
			}
			return null;
		}

		private void HandleRowEditingChanged(bool isEditing)
		{
			_isRowEditing = isEditing;
			if (!isEditing)
			{
				_rowEditingEndedFrame = Time.frameCount;
			}
		}

		private int FindSelectedRowIndex()
		{
			GameObject current = UISelection.Current;
			if (!(current != null))
			{
				return -1;
			}
			return FindRowIndex(current);
		}

		private int FindRowIndex(GameObject target)
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				if (_rows[i] != null && target.transform.IsChildOf(_rows[i].transform))
				{
					return i;
				}
			}
			return -1;
		}

		private void RebuildRows()
		{
			int num = FindSelectedRowIndex();
			bool flag = num >= 0 && _rows[num].RowSelectable != null && _rows[num].RowSelectable.IsEditing;
			for (int i = 0; i < _rows.Count; i++)
			{
				if (_rows[i] != null)
				{
					UnityEngine.Object.Destroy(_rows[i].gameObject);
				}
			}
			_rows.Clear();
			_keybindRows.Clear();
			BuildRows();
			SelectTab(_currentTab);
			if (num < 0 || num >= _rows.Count)
			{
				return;
			}
			SettingsRowSelectable rowSelectable = _rows[num].RowSelectable;
			if (!(rowSelectable == null))
			{
				UISelection.Select(rowSelectable.gameObject);
				if (flag)
				{
					rowSelectable.SetEditing(isEditing: true);
				}
			}
		}

		private void BuildRows()
		{
			BuildGeneralTab();
			BuildGraphicTab();
			BuildSoundTab();
			BuildControlTab();
			foreach (SettingsRowBase row in _rows)
			{
				LanguageFollow.AllLabels(row);
			}
		}

		private T Row<T>(SettingsRowType rowType, SettingsTabType tab, string titleKey) where T : SettingsRowBase
		{
			T val = _factory.CreateRow<T>(rowType, tab, _content);
			val.SetTitle(Loc.Get(titleKey));
			AddRow(val);
			return val;
		}

		private void AddRow(SettingsRowBase row)
		{
			_rows.Add(row);
			if (row.RowSelectable != null)
			{
				row.RowSelectable.OnTabStepRequested += StepTab;
				row.RowSelectable.OnEditingChanged += HandleRowEditingChanged;
			}
		}

		private void BuildGeneralTab()
		{
			NextPreviousSettingsRow nextPreviousSettingsRow = Row<NextPreviousSettingsRow>(SettingsRowType.NextPrevious, SettingsTabType.General, "settings.language.label");
			IReadOnlyList<LanguageInfo> languages = Loc.System?.AvailableLanguages ?? Array.Empty<LanguageInfo>();
			List<string> list = new List<string>(languages.Count);
			int num = 0;
			for (int i = 0; i < languages.Count; i++)
			{
				list.Add(languages[i].NativeName);
				if (languages[i].Code == GameSettings.Language)
				{
					num = i;
				}
			}
			nextPreviousSettingsRow.SetOptions(list, num);
			LanguageNameText languageName = null;
			if (nextPreviousSettingsRow.ValueText != null && languages.Count > 0)
			{
				languageName = nextPreviousSettingsRow.ValueText.gameObject.AddComponent<LanguageNameText>();
				languageName.SetLanguage(languages[num]);
			}
			nextPreviousSettingsRow.OnIndexChanged += (int index) =>
			{
				if (index >= 0 && index < languages.Count)
				{
					if (languageName != null)
					{
						languageName.SetLanguage(languages[index]);
					}
					GameSettings.Language = languages[index].Code;
				}
			};
			ButtonSettingsRow buttonSettingsRow = Row<ButtonSettingsRow>(SettingsRowType.Button, SettingsTabType.General, "settings.feedback.label");
			buttonSettingsRow.SetButtonText(Loc.Get("settings.feedback.submit"));
			buttonSettingsRow.OnClicked += OpenFeedbackSurvey;
			ButtonSettingsRow buttonSettingsRow2 = Row<ButtonSettingsRow>(SettingsRowType.Button, SettingsTabType.General, "settings.btn.reset");
			buttonSettingsRow2.SetButtonText(Loc.Get("settings.btn.reset"));
			buttonSettingsRow2.OnClicked += ResetAll;
		}

		private void OpenFeedbackSurvey()
		{
			if (this.TryGetSystem<SurveySystem, CoreSystemHandler>(out var system))
			{
				system.OpenSurvey();
			}
		}

		private void ResetAll()
		{
			GameSettings.ResetDefaults();
			KeyBindings.ResetDefaults();
			ApplyAudio();
			GameSettings.ApplyGraphicsQuality();
			ApplyCameraDistance(GameSettings.CameraMaxDistance);
			QueueRebuild();
		}

		private void BuildGraphicTab()
		{
			if (!Application.isMobilePlatform)
			{
				(string Level, string LabelKey)[] options = GameSettings.GraphicsQualityOptions;
				List<string> list = new List<string>(options.Length);
				int startIndex = 0;
				for (int i = 0; i < options.Length; i++)
				{
					list.Add(Loc.Get(options[i].LabelKey));
					if (options[i].Level == GameSettings.GraphicsQuality)
					{
						startIndex = i;
					}
				}
				NextPreviousSettingsRow nextPreviousSettingsRow = Row<NextPreviousSettingsRow>(SettingsRowType.NextPrevious, SettingsTabType.Graphic, "settings.graphic.quality");
				nextPreviousSettingsRow.SetOptions(list, startIndex);
				nextPreviousSettingsRow.OnIndexChanged += (int index) =>
				{
					if (index >= 0 && index < options.Length)
					{
						GameSettings.GraphicsQuality = options[index].Level;
					}
				};
			}
			ToggleSettingsRow toggleSettingsRow = Row<ToggleSettingsRow>(SettingsRowType.Toggle, SettingsTabType.Graphic, "settings.graphic.camerashake");
			toggleSettingsRow.SetValue(GameSettings.CameraShakeEnabled);
			toggleSettingsRow.OnToggled += (bool value) =>
			{
				GameSettings.CameraShakeEnabled = value;
			};
			SliderSettingsRow sliderSettingsRow = Row<SliderSettingsRow>(SettingsRowType.Slider, SettingsTabType.Graphic, "settings.graphic.cameramaxdist.title");
			sliderSettingsRow.SetRange(15f, 45f, GameSettings.CameraMaxDistance, (float v) => $"{v:0}m");
			sliderSettingsRow.OnValueChanged += (float value) =>
			{
				GameSettings.CameraMaxDistance = value;
				ApplyCameraDistance(value);
			};
		}

		private static void ApplyCameraDistance(float value)
		{
			if (BoatCameraRig.Active != null)
			{
				BoatCameraRig.Active.DistanceMax = value;
			}
		}

		private void BuildSoundTab()
		{
			AddVolumeRow("settings.audio.master.label", GameSettings.MasterVolume, (float v) =>
			{
				GameSettings.MasterVolume = v;
				ApplyAudio();
			});
			AddVolumeRow("settings.audio.music.label", GameSettings.MusicVolume, (float v) =>
			{
				GameSettings.MusicVolume = v;
				ApplyAudio();
			});
			AddVolumeRow("settings.audio.sfx.label", GameSettings.SFXVolume, (float v) =>
			{
				GameSettings.SFXVolume = v;
				ApplyAudio();
			});
			AddVolumeRow("settings.audio.fart.label", GameSettings.FartVolume, (float v) =>
			{
				GameSettings.FartVolume = v;
				ApplyAudio();
			});
		}

		private void AddVolumeRow(string titleKey, float current, Action<float> apply)
		{
			SliderSettingsRow sliderSettingsRow = Row<SliderSettingsRow>(SettingsRowType.Slider, SettingsTabType.Sound, titleKey);
			sliderSettingsRow.SetRange(0f, 1f, current, (float v) => Mathf.RoundToInt(v * 100f).ToString());
			sliderSettingsRow.OnValueChanged += (float value) =>
			{
				apply(value);
			};
		}

		private static void ApplyAudio()
		{
			GameSettings.ApplyAudio();
		}

		private void BuildControlTab()
		{
			Row<HeaderSettingsRow>(SettingsRowType.Header, SettingsTabType.Control, "settings.controls.keyboard_mouse_title");
			SliderSettingsRow sliderSettingsRow = Row<SliderSettingsRow>(SettingsRowType.Slider, SettingsTabType.Control, "settings.sens.mouse.title");
			sliderSettingsRow.SetRange(0.05f, 2f, GameSettings.MouseSensitivity, (float v) => $"{v:0.00}");
			sliderSettingsRow.OnValueChanged += (float value) =>
			{
				GameSettings.MouseSensitivity = value;
			};
			ToggleSettingsRow toggleSettingsRow = Row<ToggleSettingsRow>(SettingsRowType.Toggle, SettingsTabType.Control, "settings.toggle.invert_mouse");
			toggleSettingsRow.SetValue(GameSettings.InvertMouse);
			toggleSettingsRow.OnToggled += (bool value) =>
			{
				GameSettings.InvertMouse = value;
			};
			BuildKeybindRows("Keyboard&Mouse");
			Row<HeaderSettingsRow>(SettingsRowType.Header, SettingsTabType.Control, "settings.controls.controller_title");
			SliderSettingsRow sliderSettingsRow2 = Row<SliderSettingsRow>(SettingsRowType.Slider, SettingsTabType.Control, "settings.sens.pad.title");
			sliderSettingsRow2.SetRange(0.2f, 2f, GameSettings.PadSensitivity, (float v) => $"{v:0.00}");
			sliderSettingsRow2.OnValueChanged += (float value) =>
			{
				GameSettings.PadSensitivity = value;
			};
			BuildKeybindRows("Gamepad");
		}

		private void BuildKeybindRows(string controlScheme)
		{
			string description = Loc.Get("settings.controls.rebind_hint");
			for (int i = 0; i < 3; i++)
			{
				KeybindSettingsRow keybindSettingsRow = _factory.CreateRow<KeybindSettingsRow>(SettingsRowType.Keybind, SettingsTabType.Control, _content);
				KeybindRowEntry entry = new KeybindRowEntry
				{
					Action = (GameAction)i,
					ControlScheme = controlScheme,
					Row = keybindSettingsRow
				};
				keybindSettingsRow.SetTitle(KeyBindings.Label(entry.Action));
				keybindSettingsRow.SetDescription(description);
				RefreshKeybindRow(entry);
				keybindSettingsRow.OnRebindRequested += () =>
				{
					RebindRowAsync(entry).Forget();
				};
				_keybindRows.Add(entry);
				AddRow(keybindSettingsRow);
			}
		}

		private async UniTaskVoid RebindRowAsync(KeybindRowEntry entry)
		{
			if (KeyBindings.IsRebinding)
			{
				KeyBindings.CancelRebind();
			}
			await UniTask.Yield(base.destroyCancellationToken);
			entry.Row.SetListening(isListening: true);
			await KeyBindings.RebindAsync(entry.Action, entry.ControlScheme, base.destroyCancellationToken);
			if (entry.Row != null)
			{
				entry.Row.SetListening(isListening: false);
			}
			RefreshKeybindRows();
		}

		private void RefreshKeybindRows()
		{
			foreach (KeybindRowEntry keybindRow in _keybindRows)
			{
				if (keybindRow.Row != null)
				{
					RefreshKeybindRow(keybindRow);
				}
			}
		}

		private static void RefreshKeybindRow(KeybindRowEntry entry)
		{
			InputPromptContent inputPromptContent = KeyBindings.Prompt(entry.Action, entry.ControlScheme);
			entry.Row.SetKey(inputPromptContent.Icon, KeyBindings.DisplayName(entry.Action, entry.ControlScheme));
		}
	}
}
