using FartTogether.Raft;
using UnityEngine;

namespace FartTogether.Track
{
    /// <summary>
    /// Massive trigger plane placed under the world to catch falling rafts.
    /// Replicates VoidKill from:
    /// - _reference/PuffPirates/Track/LevelBuilder.cs (lines 1183-1192)
    /// - _reference/PuffPirates/Track/RaceZone.cs (lines 66-68, 76-86, 88-102)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class VoidKillZone : MonoBehaviour
    {
        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerVoidRespawn(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TriggerVoidRespawn(other);
        }

        private void TriggerVoidRespawn(Collider other)
        {
            RaftRespawn respawn = other.GetComponentInParent<RaftRespawn>();
            if (respawn != null)
            {
                string hierarchy = RaftRespawn.GetHierarchyPath(transform);
                respawn.Respawn("VoidKill", hierarchy, GetComponent<Collider>());
            }
        }
    }
}
