using System.Collections.Generic;
using DG.Tweening;
using GamesMrkt.Core;
using PuffPirates.Boats;
using PuffPirates.Cosmetics;
using PuffPirates.Networking;
using PuffPirates.Players;
using PuffPirates.Systems;
using PuffPirates.Track;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Gameplay
{
	public class GameplayHudPanel : MonoBehaviour
	{
		public enum MarkSpacing
		{
			ByDistance = 0,
			Uniform = 1
		}

		[Header("Gaz")]
		[Tooltip("Slider — normalizedValue 0..1 yazılır, yani Min/Max Value ne olursa olsun çalışır.")]
		[SerializeField]
		private Slider _gasSlider;

		[Tooltip("Slider'ın Fill Image'ı. Boş bırakılırsa renk hiç değişmez, bar senin kurduğun renkte kalır.")]
		[SerializeField]
		private Image _gasFill;

		[SerializeField]
		private Gradient _gasGradient = new Gradient
		{
			colorKeys = new GradientColorKey[3]
			{
				new GradientColorKey(new Color(0.93f, 0.23f, 0.23f), 0f),
				new GradientColorKey(new Color(0.98f, 0.76f, 0.18f), 0.45f),
				new GradientColorKey(new Color(0.25f, 0.62f, 0.96f), 1f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		[Header("Ölüm sayısı")]
		[SerializeField]
		private TMP_Text _deathLabel;

		[Tooltip("Shaken on every death. Usually the counter's background panel. Leave empty to skip the shake.")]
		[SerializeField]
		private RectTransform _deathBackground;

		[Tooltip("Color the counter snaps to on death, then fades back to its original color.")]
		[SerializeField]
		private Color _deathFlashColor = new Color(0.93f, 0.23f, 0.23f);

		[SerializeField]
		[Min(0f)]
		private float _deathFlashDuration = 0.5f;

		[SerializeField]
		[Min(0f)]
		private float _deathShakeDuration = 0.4f;

		[SerializeField]
		[Min(0f)]
		private float _deathShakeStrength = 12f;

		[SerializeField]
		[Min(1f)]
		private int _deathShakeVibrato = 20;

		[Header("Süre")]
		[SerializeField]
		private TMP_Text _timerLabel;

		[Header("Progress (çubuk + üstünde kayan ikon + checkpoint işaretleri)")]
		[Tooltip("Çubuğun kendisi. İkonun ve işaretlerin ebeveyni.")]
		[SerializeField]
		private RectTransform _progressTrack;

		[Tooltip("Çubuk üzerinde kayan ikon. Anchor: sol-orta (0, 0.5). Yalnız anchoredPosition.x yazılır.")]
		[SerializeField]
		private RectTransform _progressIcon;

		[Tooltip("TEK bir checkpoint işareti — pistteki checkpoint sayısı kadar kopyası çıkarılır. Sayıyı sen belirlemezsin, RaceTrack'ten okunur.")]
		[SerializeField]
		private RectTransform _checkpointMarkPrefab;

		[Tooltip("ByDistance: pistteki gerçek mesafeye göre (ikon sabit hızda giderken çubukta da sabit hızda ilerler). Uniform: eşit aralık (daha derli toplu görünür).")]
		[SerializeField]
		private MarkSpacing _markSpacing;

		[Header("Karakter")]
		[SerializeField]
		private Image _characterPortrait;

		private ConnectionSystem _connection;

		private CosmeticRelaySystem _cosmetics;

		private NetworkBoat _deathSource;

		private Color _deathLabelRestColor;

		private Vector2 _deathBackgroundRestPos;

		private Tween _deathFlashTween;

		private Tween _deathShakeTween;

		private Color _shownGasColor = new Color(0f, 0f, 0f, 0f);

		private int _shownDeaths = int.MinValue;

		private int _shownCentis = int.MinValue;

		private int _shownSkin = int.MinValue;

		private readonly List<RectTransform> _spawnedMarks = new List<RectTransform>();

		private float[] _cumulative;

		private int _builtCount = -1;

		private bool _builtLooping;

		private Transform _builtFinish;

		private float _builtWidth = -1f;

		private const float GasBarSmoothing = 15f;

		private PiratePlayer _gasSource;

		private float _shownGas;

		private void Awake()
		{
			if (_deathLabel != null)
			{
				_deathLabelRestColor = _deathLabel.color;
			}
			if (_deathBackground != null)
			{
				_deathBackgroundRestPos = _deathBackground.anchoredPosition;
			}
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
				this.TryGetSystem<CosmeticRelaySystem, CoreSystemHandler>(out _cosmetics);
			}
		}

		private void Update()
		{
			PiratePlayer me = ((_connection != null) ? _connection.LocalPirate : null);
			NetworkBoat boat = ((_connection != null) ? _connection.LocalBoat : null);
			RaceState instance = RaceState.Instance;
			RefreshCharacter();
			RefreshGas(me);
			RefreshDeaths(boat);
			RefreshTimer(instance);
			RefreshProgress(boat, instance);
		}

		private void RefreshCharacter()
		{
			if (!(_cosmetics == null) && !(_characterPortrait == null))
			{
				int num = _cosmetics.Equipped(CosmeticSlot.CharacterSkin);
				if (num != _shownSkin)
				{
					_shownSkin = num;
					Sprite gameIcon = _cosmetics.GetGameIcon(num);
					_characterPortrait.sprite = gameIcon;
					_characterPortrait.enabled = gameIcon != null;
				}
			}
		}

		private void RefreshGas(PiratePlayer me)
		{
			if (!(me == null) && !(me.Gas == null))
			{
				float num = Mathf.Max(1f, me.Gas.MaxGas);
				float num2 = Mathf.Clamp01(me.Gas.GasEnergy / num);
				if (me != _gasSource)
				{
					_gasSource = me;
					_shownGas = num2;
				}
				else
				{
					_shownGas = Mathf.Lerp(_shownGas, num2, 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime));
				}
				if (_gasSlider != null)
				{
					_gasSlider.normalizedValue = _shownGas;
				}
				RefreshGasColor(_shownGas);
			}
		}

		private void RefreshGasColor(float t)
		{
			if (!(_gasFill == null))
			{
				Color color = _gasGradient.Evaluate(t);
				if (!ApproximatelySameColor(color, _shownGasColor))
				{
					_shownGasColor = color;
					_gasFill.color = color;
				}
			}
		}

		private static bool ApproximatelySameColor(Color a, Color b)
		{
			if (Mathf.Abs(a.r - b.r) < 1f / 256f && Mathf.Abs(a.g - b.g) < 1f / 256f && Mathf.Abs(a.b - b.b) < 1f / 256f)
			{
				return Mathf.Abs(a.a - b.a) < 1f / 256f;
			}
			return false;
		}

		private void RefreshDeaths(NetworkBoat boat)
		{
			if (boat != _deathSource)
			{
				if (_deathSource != null)
				{
					_deathSource.Died -= OnLocalBoatDied;
				}
				_deathSource = boat;
				if (_deathSource != null)
				{
					_deathSource.Died += OnLocalBoatDied;
				}
			}
			int num = ((boat != null) ? boat.Deaths : 0);
			if (num != _shownDeaths)
			{
				_shownDeaths = num;
				if (_deathLabel != null)
				{
					_deathLabel.text = num.ToString();
				}
			}
		}

		private void OnLocalBoatDied(Vector3 position, DeathCause cause)
		{
			PlayDeathFeedback();
		}

		private void PlayDeathFeedback()
		{
			if (_deathLabel != null)
			{
				_deathFlashTween?.Kill();
				_deathLabel.color = _deathFlashColor;
				_deathFlashTween = DOTween.To(() => _deathLabel.color, (Color c) =>
				{
					_deathLabel.color = c;
				}, _deathLabelRestColor, _deathFlashDuration).SetEase(Ease.InQuad).SetLink(_deathLabel.gameObject);
			}
			if (_deathBackground != null)
			{
				_deathShakeTween?.Kill();
				_deathBackground.anchoredPosition = _deathBackgroundRestPos;
				_deathShakeTween = _deathBackground.DOShakeAnchorPos(_deathShakeDuration, _deathShakeStrength, _deathShakeVibrato).OnComplete(() =>
				{
					_deathBackground.anchoredPosition = _deathBackgroundRestPos;
				}).SetLink(_deathBackground.gameObject);
			}
		}

		private void RefreshTimer(RaceState race)
		{
			if (!(_timerLabel == null))
			{
				int num = (int)(((race != null) ? Mathf.Max(0f, race.RunElapsedSeconds) : 0f) * 100f);
				if (num != _shownCentis)
				{
					_shownCentis = num;
					int num2 = num / 6000;
					int num3 = num / 100 % 60;
					int num4 = num % 100;
					_timerLabel.text = $"{num2:00}:{num3:00}.{num4:00}";
				}
			}
		}

		private void RefreshProgress(NetworkBoat boat, RaceState race)
		{
			if (_progressTrack == null)
			{
				return;
			}
			RaceTrack instance = RaceTrack.Instance;
			int num = ((instance != null) ? instance.CheckpointCount : 0);
			if (num <= 0)
			{
				return;
			}
			bool flag = race != null && LevelBuilder.IsLoopingMap(race.MapId);
			Transform transform = (flag ? null : instance.FinishPoint);
			float width = _progressTrack.rect.width;
			if (width <= 0f)
			{
				return;
			}
			if (_builtCount != num || _builtLooping != flag || _builtFinish != transform)
			{
				BuildMarks(instance, num, flag, transform);
			}
			if (_cumulative == null)
			{
				return;
			}
			if (!Mathf.Approximately(_builtWidth, width))
			{
				RepositionMarks(width);
			}
			if (_progressIcon == null || boat == null)
			{
				return;
			}
			float num2;
			if (boat.FinishRank != 0)
			{
				num2 = 1f;
			}
			else
			{
				int num3 = Mathf.Clamp(boat.CurrentCheckpointId, 0, num - 1);
				Transform checkpoint = instance.GetCheckpoint(num3);
				Transform transform2;
				float b;
				if (num3 + 1 < num)
				{
					transform2 = instance.GetCheckpoint(num3 + 1);
					b = _cumulative[num3 + 1];
				}
				else if (flag)
				{
					transform2 = instance.GetCheckpoint(0);
					b = 1f;
				}
				else if (transform != null)
				{
					transform2 = transform;
					b = 1f;
				}
				else
				{
					transform2 = null;
					b = 1f;
				}
				num2 = ((transform2 == null) ? 1f : Mathf.Lerp(_cumulative[num3], b, SegmentFraction(boat, checkpoint, transform2)));
			}
			float num4 = num2 * width;
			Vector2 anchoredPosition = _progressIcon.anchoredPosition;
			if (!Mathf.Approximately(anchoredPosition.x, num4))
			{
				_progressIcon.anchoredPosition = new Vector2(num4, anchoredPosition.y);
			}
		}

		private void BuildMarks(RaceTrack track, int total, bool looping, Transform finish)
		{
			_builtCount = total;
			_builtLooping = looping;
			_builtFinish = finish;
			_cumulative = null;
			int num = (looping ? total : (total - 1 + ((finish != null) ? 1 : 0)));
			if (num <= 0)
			{
				ClearMarks();
				return;
			}
			float[] array = new float[total];
			if (_markSpacing == MarkSpacing.Uniform)
			{
				for (int i = 0; i < total; i++)
				{
					array[i] = (float)i / (float)num;
				}
			}
			else
			{
				float num2 = 0f;
				array[0] = 0f;
				for (int j = 0; j < num; j++)
				{
					Transform checkpoint = track.GetCheckpoint(j);
					Transform transform = ((j + 1 < total) ? track.GetCheckpoint(j + 1) : (looping ? track.GetCheckpoint(0) : finish));
					num2 += ((checkpoint != null && transform != null) ? Vector3.Distance(checkpoint.position, transform.position) : 0f);
					if (j + 1 < total)
					{
						array[j + 1] = num2;
					}
				}
				if (num2 <= 0.0001f)
				{
					for (int k = 0; k < total; k++)
					{
						array[k] = (float)k / (float)num;
					}
				}
				else
				{
					for (int l = 0; l < total; l++)
					{
						array[l] /= num2;
					}
				}
			}
			_cumulative = array;
			SpawnMarks(total);
		}

		private void SpawnMarks(int total)
		{
			ClearMarks();
			if (!(_checkpointMarkPrefab == null))
			{
				float num = (_builtWidth = _progressTrack.rect.width);
				float y = _checkpointMarkPrefab.anchoredPosition.y;
				for (int i = 0; i < total; i++)
				{
					RectTransform rectTransform = Object.Instantiate(_checkpointMarkPrefab, _progressTrack);
					rectTransform.gameObject.SetActive(value: true);
					rectTransform.name = $"checkpoint_mark_{i}";
					rectTransform.anchorMin = new Vector2(0f, rectTransform.anchorMin.y);
					rectTransform.anchorMax = new Vector2(0f, rectTransform.anchorMax.y);
					rectTransform.anchoredPosition = new Vector2(_cumulative[i] * num, y);
					_spawnedMarks.Add(rectTransform);
				}
				if (_checkpointMarkPrefab.gameObject.scene.IsValid())
				{
					_checkpointMarkPrefab.gameObject.SetActive(value: false);
				}
			}
		}

		private void RepositionMarks(float width)
		{
			_builtWidth = width;
			for (int i = 0; i < _spawnedMarks.Count && i < _cumulative.Length; i++)
			{
				RectTransform rectTransform = _spawnedMarks[i];
				if (!(rectTransform == null))
				{
					rectTransform.anchoredPosition = new Vector2(_cumulative[i] * width, rectTransform.anchoredPosition.y);
				}
			}
		}

		private void ClearMarks()
		{
			for (int i = 0; i < _spawnedMarks.Count; i++)
			{
				if (_spawnedMarks[i] != null)
				{
					Object.Destroy(_spawnedMarks[i].gameObject);
				}
			}
			_spawnedMarks.Clear();
		}

		private static float SegmentFraction(NetworkBoat boat, Transform a, Transform b)
		{
			if (a == null || b == null)
			{
				return 0f;
			}
			Vector3 rhs = b.position - a.position;
			float sqrMagnitude = rhs.sqrMagnitude;
			if (sqrMagnitude < 0.0001f)
			{
				return 0f;
			}
			return Mathf.Clamp01(Vector3.Dot(boat.transform.position - a.position, rhs) / sqrMagnitude);
		}

		private void OnDestroy()
		{
			if (_deathSource != null)
			{
				_deathSource.Died -= OnLocalBoatDied;
			}
			_deathFlashTween?.Kill();
			_deathShakeTween?.Kill();
			ClearMarks();
		}
	}
}
