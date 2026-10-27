using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FartTogether.Obstacles
{
    /// <summary>
    /// Replicates ImportedObstacleAnimation.cs from the original game (_reference/PuffPirates/Obstacles/ImportedObstacleAnimation.cs).
    /// Drives mechanical obstacles (tipping hammers, double sweepers, rotor crosses, windmills, pendulum maces)
    /// by sampling their authentic FBX AnimationClips on their inner animation root (Visual / GM_3D_ObstacleF*).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ObstacleAnimationController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("The authentic AnimationClip for this obstacle.")]
        [SerializeField] private AnimationClip _clip;

        [Tooltip("Inner root transform the animation tracks apply to. In the original game, this is the 'Visual' or 'GM_3D_ObstacleF*' child.")]
        [SerializeField] private GameObject _animationRoot;

        [Tooltip("Playback speed multiplier (from original ImportedObstacleAnimation._playbackSpeed).")]
        [SerializeField] private float _playbackSpeed = 1.0f;

        [Tooltip("Deterministic phase offset in seconds to de-sync identical obstacles.")]
        [SerializeField] private float _phaseSeconds = 0.0f;

        [Tooltip("Whether the obstacle begins animating immediately.")]
        [SerializeField] private bool _startActive = true;

        [Tooltip("Animation wrap mode (Loop in original game).")]
        [SerializeField] private WrapMode _wrapMode = WrapMode.Loop;

        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;
        private bool _isGraphValid;

        public AnimationClip Clip { get => _clip; set { _clip = value; RebuildGraph(); } }
        public GameObject AnimationRoot { get => _animationRoot; set { _animationRoot = value; RebuildGraph(); } }
        public float PlaybackSpeed { get => _playbackSpeed; set => _playbackSpeed = Mathf.Max(0.01f, value); }
        public float PhaseSeconds { get => _phaseSeconds; set => _phaseSeconds = Mathf.Max(0f, value); }
        public WrapMode WrapMode { get => _wrapMode; set => _wrapMode = value; }

        private void Awake()
        {
            ResolveAnimationRoot();
        }

        private void OnEnable()
        {
            ResolveAnimationRoot();
            if (_clip != null)
            {
                RebuildGraph();
            }
        }

        private void OnDisable()
        {
            DestroyGraph();
        }

        private void OnDestroy()
        {
            DestroyGraph();
        }

        private void Update()
        {
            if (!_startActive || _clip == null || _clip.length <= 0.0001f) return;

            float time = Application.isPlaying ? Time.time : (float)EditorApplicationTime();
            float sampleTime = Mathf.Repeat(time * _playbackSpeed + _phaseSeconds, _clip.length);

            GameObject target = GetTarget();

            if (_isGraphValid && _graph.IsValid())
            {
                _playable.SetTime(sampleTime);
                _graph.Evaluate();
            }
            else if (_clip.legacy || !Application.isPlaying)
            {
                // Original logic: _clip.SampleAnimation(_animationRoot, num);
                _clip.SampleAnimation(target, sampleTime);
            }
            else
            {
                RebuildGraph();
            }
        }

        public void Configure(AnimationClip clip, GameObject root, float speed = 1f, float phase = 0f)
        {
            _clip = clip;
            _animationRoot = root;
            ResolveAnimationRoot();
            _playbackSpeed = Mathf.Max(0.01f, speed);
            _phaseSeconds = Mathf.Max(0f, phase);
            RebuildGraph();
        }

        private void ResolveAnimationRoot()
        {
            if (_animationRoot == null || _animationRoot == gameObject)
            {
                // Check for Visual or visual child
                Transform r = transform.Find("Visual") ?? transform.Find("visual");
                if (r == null)
                {
                    foreach (Transform child in transform)
                    {
                        if (child.name.StartsWith("GM_3D_ObstacleF", StringComparison.OrdinalIgnoreCase))
                        {
                            r = child;
                            break;
                        }
                    }
                }
                _animationRoot = r != null ? r.gameObject : gameObject;
            }

            // Disable any conflicting empty/unassigned legacy Animation component from FBX import
            if (_animationRoot != null && _animationRoot.TryGetComponent<Animation>(out var legacyAnim))
            {
                legacyAnim.playAutomatically = false;
                legacyAnim.enabled = false;
            }
        }

        private GameObject GetTarget()
        {
            if (_animationRoot == null) ResolveAnimationRoot();
            return _animationRoot != null ? _animationRoot : gameObject;
        }

        private void RebuildGraph()
        {
            DestroyGraph();

            if (_clip == null || !Application.isPlaying) return;

            GameObject target = GetTarget();

            // Disable conflicting legacy Animation component if present
            if (target.TryGetComponent<Animation>(out var legacyAnim))
            {
                legacyAnim.playAutomatically = false;
                legacyAnim.enabled = false;
            }

            // If marked legacy, use SampleAnimation directly without Playables (exact matching original line 148)
            if (_clip.legacy)
            {
                return;
            }

            try
            {
                if (!target.TryGetComponent<Animator>(out var animator))
                {
                    animator = target.AddComponent<Animator>();
                }
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.fireEvents = false;

                _graph = PlayableGraph.Create(gameObject.name + "_ObstacleAnim");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                _playable = AnimationClipPlayable.Create(_graph, _clip);
                _playable.SetApplyFootIK(false);

                AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "ObstacleOutput", animator);
                output.SetSourcePlayable(_playable);

                _graph.Play();
                _isGraphValid = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ObstacleAnimationController] PlayableGraph setup on {target.name} failed ({ex.Message}). Will use SampleAnimation fallback.");
                DestroyGraph();
            }
        }

        private void DestroyGraph()
        {
            if (_isGraphValid && _graph.IsValid())
            {
                _graph.Destroy();
            }
            _isGraphValid = false;
        }

        private double EditorApplicationTime()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.timeSinceStartup;
#else
            return 0;
#endif
        }
    }
}
