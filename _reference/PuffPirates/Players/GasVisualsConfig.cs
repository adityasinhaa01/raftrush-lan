using GamesMrkt.Audio;
using UnityEngine;

namespace PuffPirates.Players
{
	[CreateAssetMenu(fileName = "GasVisualsConfig", menuName = "GamesMrkt/Pirate/GasVisualsConfig")]
	public class GasVisualsConfig : ScriptableObject
	{
		[Header("Sound (GamesMrkt.Audio) — gas categories")]
		[Tooltip("Clip pool played while fuel is high (above GasAudioSettings.Xf1Start).")]
		[SerializeField]
		private SoundData _gasHighSound;

		[Tooltip("Clip pool played while fuel is at a medium level.")]
		[SerializeField]
		private SoundData _gasLowSound;

		[Tooltip("Clip pool played while fuel is low (below GasAudioSettings.Xf2Start).")]
		[SerializeField]
		private SoundData _gasDampingSound;

		[Header("Tap burst (bas-çek one-shot)")]
		[Tooltip("Gaz kısa süre basılıp hemen bırakıldığında (bas-çek / tap, ramp sistemine hiç girmeden tek seferlik force uygulandığında) çalınan tek atımlık ses. Birden fazla klip varsa rastgele seçilir.")]
		[SerializeField]
		private SoundData _gasTapSound;

		[Header("Sputter tık tık (low fuel one-shot)")]
		[Tooltip("Gaz jeti son yakıtta kesik kesik (sputter) ateşlenirken HER burst'te çalınan tek atımlık ses. Birden fazla klip varsa rastgele seçilir (SoundData'nın kendi özelliği). Boşsa ses çıkmaz.")]
		[SerializeField]
		private SoundData _sputterClickSound;

		[Header("Head-pop while gassing (Item 9)")]
		[Tooltip("Head swells to this ratio while gassing (1 = off, 1.4 = 40% bigger).")]
		[SerializeField]
		private float _headPopScale = 1.4f;

		[Tooltip("Swell/settle speed. Higher = instant pop, lower = smooth.")]
		[SerializeField]
		private float _headPopLerp = 16f;

		public SoundData GasHighSound => _gasHighSound;

		public SoundData GasLowSound => _gasLowSound;

		public SoundData GasDampingSound => _gasDampingSound;

		public SoundData GasTapSound => _gasTapSound;

		public SoundData SputterClickSound => _sputterClickSound;

		public float HeadPopScale => _headPopScale;

		public float HeadPopLerp => _headPopLerp;
	}
}
