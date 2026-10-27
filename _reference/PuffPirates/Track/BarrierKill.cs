using System.Text;
using PuffPirates.Boats;
using PuffPirates.Obstacles;
using UnityEngine;

namespace PuffPirates.Track
{
	[RequireComponent(typeof(Collider))]
	[DisallowMultipleComponent]
	public class BarrierKill : MonoBehaviour
	{
		public static bool GloballyEnabled = true;

		[Tooltip("Respawn sonrası dokunulmazlık (sn). Duvar dibinde respawn döngüsünü engeller.")]
		[SerializeField]
		private float _invulnSeconds = 2f;

		[SerializeField]
		private bool _respawnAtStart = true;

		[Tooltip("Death cause reported to the boat (picks the death sound). None = automatic: solid collision → Wall, trigger → Obstacle.")]
		[SerializeField]
		private DeathCause _cause;

		public static bool LogKills = false;

		public void Configure(bool respawnAtStart, float invulnSeconds = 2f, DeathCause cause = DeathCause.None)
		{
			_respawnAtStart = respawnAtStart;
			_invulnSeconds = invulnSeconds;
			_cause = cause;
		}

		private void OnCollisionEnter(Collision c)
		{
			bool flag = c.contactCount > 0;
			ContactPoint contactPoint = (flag ? c.GetContact(0) : default(ContactPoint));
			Kill(c.collider, flag ? contactPoint.thisCollider : GetComponent<Collider>(), flag ? new Vector3?(contactPoint.point) : ((Vector3?)null), "Collision", (_cause == DeathCause.None) ? DeathCause.Wall : _cause);
		}

		private void OnTriggerEnter(Collider other)
		{
			Kill(other, GetComponent<Collider>(), null, "Trigger", (_cause != DeathCause.None) ? _cause : DeathCause.Obstacle);
		}

		internal static void LogKill(string kind, Component source, Collider self, NetworkBoat boat, Vector3? contact)
		{
			if (!LogKills || boat == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(512);
			stringBuilder.Append("[KillLog] ").Append(kind).Append(" | obj=")
				.Append(PathOf(source.transform));
			stringBuilder.Append(" | ").Append(source.GetType().Name);
			if (self != null)
			{
				stringBuilder.Append(" | selfCol=").Append(self.GetType().Name).Append(" trig=")
					.Append(self.isTrigger)
					.Append(" convex=")
					.Append((self is MeshCollider { convex: var convex }) ? convex.ToString() : "-")
					.Append(" boundsSize=")
					.Append(self.bounds.size.ToString("F2"))
					.Append(" boundsCenter=")
					.Append(self.bounds.center.ToString("F2"));
				if (self is MeshCollider meshCollider2 && meshCollider2.sharedMesh != null)
				{
					stringBuilder.Append(" mesh=").Append(meshCollider2.sharedMesh.name);
				}
			}
			Vector3 position = boat.transform.position;
			stringBuilder.Append(" | boatPos=").Append(position.ToString("F2")).Append(" boatScale=")
				.Append(boat.transform.lossyScale.x.ToString("F2"));
			Rigidbody rigidbody = boat.Rigidbody;
			if (rigidbody != null)
			{
				stringBuilder.Append(" speed=").Append(rigidbody.linearVelocity.magnitude.ToString("F1"));
			}
			if (contact.HasValue)
			{
				stringBuilder.Append(" | contact=").Append(contact.Value.ToString("F2")).Append(" contactDist(boatCenter)=")
					.Append(Vector3.Distance(contact.Value, position).ToString("F2"));
			}
			if (self != null)
			{
				stringBuilder.Append(" | boatCenter→selfBounds=").Append(Mathf.Sqrt(self.bounds.SqrDistance(position)).ToString("F2"));
			}
			stringBuilder.Append(" | boatColliders=[");
			Collider[] componentsInChildren = boat.GetComponentsInChildren<Collider>(includeInactive: true);
			foreach (Collider collider in componentsInChildren)
			{
				stringBuilder.Append(collider.name).Append(':').Append(collider.GetType().Name)
					.Append(collider.isTrigger ? "(trig)" : "")
					.Append(collider.enabled ? "" : "(off)")
					.Append(' ')
					.Append(collider.bounds.size.ToString("F2"))
					.Append("; ");
			}
			stringBuilder.Append("]");
			ImportedObstacleAnimation componentInParent = source.GetComponentInParent<ImportedObstacleAnimation>();
			Transform transform = ((componentInParent != null) ? componentInParent.transform : source.transform);
			stringBuilder.Append(" | visuals(root=").Append(transform.name).Append(")=[");
			int num = 0;
			Renderer[] componentsInChildren2 = transform.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren2)
			{
				if (!(renderer is ParticleSystemRenderer) && num++ < 6)
				{
					stringBuilder.Append(renderer.name).Append((renderer.enabled && renderer.gameObject.activeInHierarchy) ? "(on)" : "(off)").Append(" c=")
						.Append(renderer.bounds.center.ToString("F2"))
						.Append(" sz=")
						.Append(renderer.bounds.size.ToString("F2"))
						.Append(" rot=")
						.Append(renderer.transform.eulerAngles.ToString("F0"))
						.Append(" dBoat=")
						.Append(Mathf.Sqrt(renderer.bounds.SqrDistance(position)).ToString("F2"))
						.Append("; ");
				}
			}
			stringBuilder.Append("] selfRot=").Append(source.transform.eulerAngles.ToString("F0")).Append(" parentRot=")
				.Append((source.transform.parent != null) ? source.transform.parent.eulerAngles.ToString("F0") : "-");
			stringBuilder.Append(" | tick=").Append(boat.IsSpawned ? boat.TimeManager.Tick.ToString() : "?").Append(" t=")
				.Append(Time.time.ToString("F2"));
			Debug.Log(stringBuilder.ToString(), source);
		}

		private static string PathOf(Transform t)
		{
			string text = t.name;
			t = t.parent;
			while (t != null)
			{
				text = t.name + "/" + text;
				t = t.parent;
			}
			return text;
		}

		private void Kill(Collider hit, Collider self, Vector3? contact, string kind, DeathCause cause)
		{
			if (!GloballyEnabled)
			{
				return;
			}
			NetworkBoat componentInParent = hit.GetComponentInParent<NetworkBoat>();
			if (componentInParent == null || !componentInParent.IsServerInitialized || componentInParent.IsInvulnerable)
			{
				return;
			}
			RaceTrack instance = RaceTrack.Instance;
			if (!(instance == null))
			{
				LogKill(kind, this, self, componentInParent, contact);
				int num = ((!_respawnAtStart) ? componentInParent.CurrentCheckpointId : 0);
				if (!instance.TryGetSafeRespawnPose(num, 1.5f, out var position, out var rotation))
				{
					Transform checkpoint = instance.GetCheckpoint(num);
					position = ((checkpoint != null) ? (checkpoint.position + Vector3.up * 1.5f) : new Vector3(0f, 2f, 0f));
					rotation = ((checkpoint != null) ? checkpoint.rotation : Quaternion.identity);
				}
				componentInParent.RespawnAt(position, rotation, _invulnSeconds, cause);
			}
		}
	}
}
