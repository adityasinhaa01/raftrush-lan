using UnityEngine;

namespace PuffPirates.Boats
{
	[CreateAssetMenu(menuName = "PuffPirates/Boat Definition", fileName = "BoatDefinition")]
	public class BoatDefinition : ScriptableObject
	{
		[Header("Kimlik")]
		public string DisplayName = "Standart Bot";

		[Header("Rigidbody")]
		public float Mass = 40f;

		public float LinearDamping = 0.4f;

		public float AngularDamping = 2f;

		public float MaxSpeed = 14f;

		[Header("Gaz / Kuvvet")]
		public float GasEfficiency = 1f;

		public float SteerTorque = 6f;

		[Header("Oyuncu Bağlantı")]
		public float AttachmentRadius = 1.4f;

		[Header("Görsel")]
		public GameObject VisualPrefab;
	}
}
