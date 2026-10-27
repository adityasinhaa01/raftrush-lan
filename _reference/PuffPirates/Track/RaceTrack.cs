using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.Modes;
using PuffPirates.Networking;
using UnityEngine;

namespace PuffPirates.Track
{
	[DisallowMultipleComponent]
	public class RaceTrack : MonoBehaviour
	{
		private readonly List<Transform> _checkpoints = new List<Transform>();

		private readonly List<Transform> _teamSpawns = new List<Transform>();

		private Transform _finish;

		private const float RespawnProbeUp = 8f;

		private const float RespawnProbeDistance = 20f;

		private const float MaxRespawnSurfaceAbove = 6f;

		private const float MaxRespawnSurfaceBelow = 12f;

		private const float MinRespawnSurfaceUpDot = 0.55f;

		private static readonly Vector2[] RespawnProbeOffsets = new Vector2[17]
		{
			Vector2.zero,
			new Vector2(0f, 1.5f),
			new Vector2(0f, -1.5f),
			new Vector2(1.5f, 0f),
			new Vector2(-1.5f, 0f),
			new Vector2(1.5f, 1.5f),
			new Vector2(-1.5f, 1.5f),
			new Vector2(1.5f, -1.5f),
			new Vector2(-1.5f, -1.5f),
			new Vector2(0f, 3f),
			new Vector2(0f, -3f),
			new Vector2(3f, 0f),
			new Vector2(-3f, 0f),
			new Vector2(0f, 5f),
			new Vector2(0f, -5f),
			new Vector2(5f, 0f),
			new Vector2(-5f, 0f)
		};

		private readonly RaycastHit[] _respawnHits = new RaycastHit[32];

		private int _hostFinishedCount;

		public static RaceTrack Instance { get; private set; }

		public int CheckpointCount => _checkpoints.Count;

		public Transform FinishPoint => _finish;

		public void RegisterTeamSpawn(int teamId, Transform t)
		{
			while (_teamSpawns.Count <= teamId)
			{
				_teamSpawns.Add(null);
			}
			_teamSpawns[teamId] = t;
		}

		public Transform GetTeamSpawn(int teamId)
		{
			if (teamId < 0 || teamId >= _teamSpawns.Count)
			{
				return null;
			}
			return _teamSpawns[teamId];
		}

		public bool TryGetTeamSpawnPose(int teamId, float lift, out Vector3 position, out Quaternion rotation)
		{
			position = default;
			rotation = Quaternion.identity;
			Transform teamSpawn = GetTeamSpawn(teamId);
			if (teamSpawn == null)
			{
				return false;
			}
			rotation = teamSpawn.rotation;
			if (!TryProjectToRespawnSurface(teamSpawn, out var surfacePoint))
			{
				position = teamSpawn.position;
				return false;
			}
			position = surfacePoint + Vector3.up * lift;
			return true;
		}

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void ResetFinishCounter()
		{
			_hostFinishedCount = 0;
		}

		public void ResetRegistry()
		{
			_checkpoints.Clear();
			_teamSpawns.Clear();
			_finish = null;
			_hostFinishedCount = 0;
		}

		public void RegisterCheckpoint(int id, Transform t)
		{
			while (_checkpoints.Count <= id)
			{
				_checkpoints.Add(null);
			}
			_checkpoints[id] = t;
		}

		public void RegisterFinish(Transform t)
		{
			_finish = t;
		}

		public Transform GetCheckpoint(int id)
		{
			if (_checkpoints.Count == 0)
			{
				return null;
			}
			for (int num = Mathf.Clamp(id, 0, _checkpoints.Count - 1); num >= 0; num--)
			{
				if (_checkpoints[num] != null)
				{
					return _checkpoints[num];
				}
			}
			return null;
		}

		public bool TryGetSafeRespawnPose(int checkpointId, float lift, out Vector3 position, out Quaternion rotation)
		{
			Transform checkpoint = GetCheckpoint(checkpointId);
			position = ((checkpoint != null) ? (checkpoint.position + Vector3.up * lift) : new Vector3(0f, lift, 0f));
			rotation = ((checkpoint != null) ? checkpoint.rotation : Quaternion.identity);
			for (int num = Mathf.Min(Mathf.Max(checkpointId, 0), _checkpoints.Count - 1); num >= 0; num--)
			{
				Transform transform = _checkpoints[num];
				if (!(transform == null) && TryProjectToRespawnSurface(transform, out var surfacePoint))
				{
					position = surfacePoint + Vector3.up * lift;
					rotation = transform.rotation;
					return true;
				}
			}
			for (int i = 0; i < _teamSpawns.Count; i++)
			{
				Transform transform2 = _teamSpawns[i];
				if (!(transform2 == null) && TryProjectToRespawnSurface(transform2, out var surfacePoint2))
				{
					position = surfacePoint2 + Vector3.up * lift;
					rotation = transform2.rotation;
					return true;
				}
			}
			return false;
		}

		private bool TryProjectToRespawnSurface(Transform anchor, out Vector3 surfacePoint)
		{
			surfacePoint = default;
			LevelDefinition componentInParent = anchor.GetComponentInParent<LevelDefinition>();
			Vector2[] respawnProbeOffsets = RespawnProbeOffsets;
			for (int i = 0; i < respawnProbeOffsets.Length; i++)
			{
				Vector2 vector = respawnProbeOffsets[i];
				int num = Physics.RaycastNonAlloc(anchor.position + anchor.right * vector.x + anchor.forward * vector.y + Vector3.up * 8f, Vector3.down, _respawnHits, 20f, -5, QueryTriggerInteraction.Ignore);
				bool flag = false;
				float num2 = float.PositiveInfinity;
				Vector3 vector2 = default;
				for (int j = 0; j < num; j++)
				{
					RaycastHit raycastHit = _respawnHits[j];
					Collider collider = raycastHit.collider;
					if (!(collider == null) && collider.enabled && collider.gameObject.activeInHierarchy && !(raycastHit.normal.y < 0.55f) && !(raycastHit.point.y > anchor.position.y + 6f) && !(raycastHit.point.y < anchor.position.y - 12f) && !(collider.attachedRigidbody != null) && !(collider.GetComponentInParent<BarrierKill>() != null) && (!(componentInParent != null) || collider.transform.IsChildOf(componentInParent.transform)))
					{
						float num3 = Mathf.Abs(raycastHit.point.y - anchor.position.y);
						if (!(num3 >= num2))
						{
							num2 = num3;
							vector2 = raycastHit.point;
							flag = true;
						}
					}
				}
				if (flag)
				{
					surfacePoint = vector2;
					return true;
				}
			}
			return false;
		}

		public void ReportFinish(NetworkBoat boat)
		{
			if (boat == null || !boat.IsServerInitialized || boat.FinishRank != 0)
			{
				return;
			}
			_hostFinishedCount++;
			boat.SetFinished(_hostFinishedCount);
			RaceState instance = RaceState.Instance;
			if (!(instance == null))
			{
				if (instance.IsParkour)
				{
					ParkurRules.Instance?.OnBoatFinished(boat, _hostFinishedCount);
				}
				else if (instance.IsRace)
				{
					YarisRules.Instance?.OnBoatFinished(boat, _hostFinishedCount);
				}
			}
		}
	}
}
