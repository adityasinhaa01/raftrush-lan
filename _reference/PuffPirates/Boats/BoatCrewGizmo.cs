using UnityEngine;

namespace PuffPirates.Boats
{
	[DisallowMultipleComponent]
	public sealed class BoatCrewGizmo : MonoBehaviour
	{
		[Header("RUNTIME crew yerlesimi (buradaki ayarlar oyunu surer)")]
		[Tooltip("Korsan halka yaricapi CARPANI. BoatSizes.AttachRadius bununla carpilir (boyutla olcekli kalir). 1 = degismez.")]
		[SerializeField]
		[Min(0.1f)]
		private float _radiusMul = 1f;

		[Tooltip("Ilk bolmenin baslangic acisi (derece). Tum sektor merkezleri bu kadar kayar. 0 = bot onu (+Z).")]
		[SerializeField]
		private float _startAngleDeg;

		[Tooltip("BOLMELER ARASI ACI (derece): komsu sektorler arasinda birakilan bosluk. 0 = bosluksuz (bitisik).")]
		[SerializeField]
		[Range(0f, 90f)]
		private float _gapDeg;

		[Header("Gizmo onizleme (edit-time)")]
		[Tooltip("Onizleme halka yaricapi (M boyut ~1.27; runtime gercek radius boyuta gore olceklenir).")]
		[SerializeField]
		private float _previewBaseRadius = 1.27f;

		[SerializeField]
		private float _waterlineY = -0.3f;

		[Tooltip("Kac korsanlik yerlesimleri onizle (or. 2 ve 3).")]
		[SerializeField]
		private int[] _previewCounts = new int[2] { 2, 3 };

		[SerializeField]
		private float _seatRadius = 0.28f;

		public static float RadiusMul { get; private set; } = 1f;

		public static float StartAngleDeg { get; private set; } = 0f;

		public static float GapDeg { get; private set; } = 0f;

		private void OnEnable()
		{
			RadiusMul = _radiusMul;
			StartAngleDeg = _startAngleDeg;
			GapDeg = _gapDeg;
		}
	}
}
