using System;
using FartTogether.Raft;
using UnityEngine;

namespace FartTogether.Track
{
    /// <summary>
    /// Trigger zone placed at the finish line.
    /// Detects the raft passing through, stops the race timer, and announces the final time.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class FinishZone : MonoBehaviour
    {
        public static event Action<float> OnRaceFinishedGlobal;

        [Header("Finish Settings")]
        [Tooltip("Color of debug gizmo wire box in editor.")]
        [SerializeField] private Color _gizmoColor = new Color(0f, 1f, 0.2f, 0.4f);

        private bool _isFinished = false;
        private float _finishTime = 0f;

        public bool IsFinished => _isFinished;
        public float FinishTime => _finishTime;

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
            if (_isFinished) return;

            RaftController raft = other.GetComponentInParent<RaftController>();
            if (raft != null)
            {
                TriggerFinish();
            }
        }

        public void TriggerFinish()
        {
            if (_isFinished) return;

            _isFinished = true;
            _finishTime = Time.timeSinceLevelLoad;

            Debug.Log($"<color=green>[FinishZone] RAFT FINISHED THE RACE! Time: {FormatTime(_finishTime)}</color>");
            OnRaceFinishedGlobal?.Invoke(_finishTime);
        }

        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainingSeconds = seconds % 60f;
            return string.Format("{0:00}:{1:00.00}", minutes, remainingSeconds);
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.color = _gizmoColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
