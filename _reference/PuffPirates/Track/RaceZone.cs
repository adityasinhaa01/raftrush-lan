using PuffPirates.Boats;
using PuffPirates.Networking;
using UnityEngine;

namespace PuffPirates.Track
{
	[RequireComponent(typeof(Collider))]
	[DisallowMultipleComponent]
	public class RaceZone : MonoBehaviour
	{
		public enum ZoneKind
		{
			Checkpoint = 0,
			Kill = 1,
			Finish = 2
		}

		[SerializeField]
		private ZoneKind _kind;

		[SerializeField]
		private int _checkpointId;

		[SerializeField]
		private float _respawnInvulnSec = 2f;

		public ZoneKind Kind => _kind;

		public int CheckpointId => _checkpointId;

		public void Configure(ZoneKind kind, int checkpointId)
		{
			_kind = kind;
			_checkpointId = checkpointId;
		}

		private static NetworkBoat BoatOf(Collider c)
		{
			Rigidbody attachedRigidbody = c.attachedRigidbody;
			if (attachedRigidbody != null)
			{
				NetworkBoat component = attachedRigidbody.GetComponent<NetworkBoat>();
				if (component != null)
				{
					return component;
				}
			}
			return c.GetComponentInParent<NetworkBoat>();
		}

		private void OnTriggerEnter(Collider other)
		{
			NetworkBoat networkBoat = BoatOf(other);
			if (!(networkBoat == null) && networkBoat.IsServerInitialized)
			{
				switch (_kind)
				{
				case ZoneKind.Checkpoint:
				{
					RaceState instance = RaceState.Instance;
					bool looping = instance != null && LevelBuilder.IsLoopingMap(instance.MapId);
					int checkpointCount = ((RaceTrack.Instance != null) ? RaceTrack.Instance.CheckpointCount : 0);
					networkBoat.PassCheckpoint(_checkpointId, checkpointCount, looping);
					break;
				}
				case ZoneKind.Kill:
					Respawn(networkBoat);
					break;
				case ZoneKind.Finish:
					RaceTrack.Instance?.ReportFinish(networkBoat);
					break;
				}
			}
		}

		private void OnTriggerStay(Collider other)
		{
			if (_kind == ZoneKind.Kill)
			{
				NetworkBoat networkBoat = BoatOf(other);
				if (!(networkBoat == null) && networkBoat.IsServerInitialized)
				{
					Respawn(networkBoat);
				}
			}
		}

		private void Respawn(NetworkBoat boat)
		{
			if (!boat.IsInvulnerable)
			{
				BarrierKill.LogKill("RaceZone.Kill", this, GetComponent<Collider>(), boat, null);
				RaceTrack instance = RaceTrack.Instance;
				if (instance == null || !instance.TryGetSafeRespawnPose(boat.CurrentCheckpointId, 1.5f, out var position, out var rotation))
				{
					Transform transform = ((instance != null) ? instance.GetCheckpoint(boat.CurrentCheckpointId) : null);
					position = ((transform != null) ? (transform.position + Vector3.up * 1.5f) : new Vector3(0f, 1.5f, 0f));
					rotation = ((transform != null) ? transform.rotation : Quaternion.identity);
				}
				boat.RespawnAt(position, rotation, _respawnInvulnSec, DeathCause.Void);
			}
		}
	}
}
