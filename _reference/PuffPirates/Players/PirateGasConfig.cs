using UnityEngine;

namespace PuffPirates.Players
{
	[CreateAssetMenu(fileName = "PirateGasConfig", menuName = "GamesMrkt/Pirate/GasConfig")]
	public class PirateGasConfig : ScriptableObject
	{
		[SerializeField]
		[Min(1f)]
		private float _maxGas = 100f;

		[SerializeField]
		[Min(0f)]
		private float _gasRegenPerSec = 18f;

		[Tooltip("Seconds regen stays paused after gas was last consumed (hold drain or tap burst). Every consume restarts the countdown.")]
		[SerializeField]
		[Min(0f)]
		private float _gasRegenDelay = 1f;

		[Tooltip("Fraction of max gas that must refill after depletion before gas can be used again (0.35 = 35%).")]
		[SerializeField]
		[Range(0.05f, 0.9f)]
		private float _gasRecoverPercent = 0.35f;

		public float MaxGas => _maxGas;

		public float GasRegenPerSec => _gasRegenPerSec;

		public float GasRegenDelay => _gasRegenDelay;

		public float GasRecoverPercent => _gasRecoverPercent;
	}
}
