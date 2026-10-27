using PuffPirates.Obstacles.Physics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PuffPirates.Obstacles
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public sealed class ImportedObstacleAnimation : PhysicsObstacleBase
	{
		[Header("FBX Animasyonu")]
		[SerializeField]
		private AnimationClip _clip;

		[Tooltip("Clip binding yollarinin kok olarak kullandigi ic FBX nesnesi.")]
		[SerializeField]
		private GameObject _animationRoot;

		[SerializeField]
		[Min(0.01f)]
		private float _playbackSpeed = 1f;

		[Tooltip("Ayni tip engellerin ayni anda donmemesi icin saniye cinsinden deterministik faz.")]
		[SerializeField]
		[Min(0f)]
		private float _phaseSeconds;

		[Header("Animation Events")]
		[Tooltip("The clip is sampled manually, so Unity never fires its Animation Events; this component fires them itself via SendMessage on this GameObject (e.g. AnimationSoundEmitter.PlaySound). Runs on every peer.")]
		[SerializeField]
		private bool _fireAnimationEvents = true;

		private const float MaxEventCatchUpSeconds = 0.25f;

		private AnimationEvent[] _events;

		private AnimationClip _eventsClip;

		private float _eventWatermark = float.NegativeInfinity;

		private PlayableGraph _graph;

		private AnimationClipPlayable _playable;

		private AnimationClip _graphClip;

		public AnimationClip Clip => _clip;

		public GameObject AnimationRoot => _animationRoot;

		public float PlaybackSpeed => _playbackSpeed;

		public float PhaseSeconds => _phaseSeconds;

		protected override void Awake()
		{
			base.Awake();
			if (_animationRoot == null)
			{
				_animationRoot = base.gameObject;
			}
			SampleAt(0f);
		}

		protected override void OnEnable()
		{
			if (Application.isPlaying)
			{
				base.OnEnable();
			}
		}

		protected override void OnDisable()
		{
			if (Application.isPlaying)
			{
				base.OnDisable();
			}
		}

		protected override void OnTick(int tick, float dt, float time)
		{
			SampleAt(time);
			FireEvents(time);
		}

		private void FireEvents(float time)
		{
			if (!_fireAnimationEvents || _clip == null || _clip.length <= 0.0001f)
			{
				return;
			}
			float eventWatermark = _eventWatermark;
			if (time <= eventWatermark)
			{
				return;
			}
			_eventWatermark = time;
			if (float.IsNegativeInfinity(eventWatermark) || time - eventWatermark > 0.25f)
			{
				return;
			}
			if (_eventsClip != _clip)
			{
				_events = _clip.events;
				_eventsClip = _clip;
			}
			if (_events.Length == 0)
			{
				return;
			}
			float length = _clip.length;
			float num = Mathf.Repeat(eventWatermark * _playbackSpeed + _phaseSeconds, length);
			float num2 = num + (time - eventWatermark) * _playbackSpeed;
			AnimationEvent[] events = _events;
			foreach (AnimationEvent animationEvent in events)
			{
				float time2 = animationEvent.time;
				if ((time2 > num && time2 <= num2) || (time2 + length > num && time2 + length <= num2))
				{
					base.gameObject.SendMessage(animationEvent.functionName, animationEvent, SendMessageOptions.DontRequireReceiver);
				}
			}
		}

		public void Configure(AnimationClip clip, GameObject animationRoot, float playbackSpeed = 1f, float phaseSeconds = 0f)
		{
			_clip = clip;
			_animationRoot = ((animationRoot != null) ? animationRoot : base.gameObject);
			_playbackSpeed = Mathf.Max(0.01f, playbackSpeed);
			_phaseSeconds = Mathf.Max(0f, phaseSeconds);
			_eventsClip = null;
			_eventWatermark = float.NegativeInfinity;
			SampleAt(0f);
		}

		private void OnDestroy()
		{
			DestroyGraph();
		}

		private void SampleAt(float time)
		{
			if (!(_clip == null) && !(_animationRoot == null) && !(_clip.length <= 0.0001f))
			{
				float num = Mathf.Repeat(time * _playbackSpeed + _phaseSeconds, _clip.length);
				if (!Application.isPlaying || _clip.legacy)
				{
					_clip.SampleAnimation(_animationRoot, num);
				}
				else if (EnsureGraph())
				{
					_playable.SetTime(num);
					_graph.Evaluate();
				}
			}
		}

		private bool EnsureGraph()
		{
			if (_graph.IsValid() && _graphClip == _clip)
			{
				return true;
			}
			DestroyGraph();
			if (!_animationRoot.TryGetComponent<Animator>(out var component))
			{
				component = _animationRoot.AddComponent<Animator>();
			}
			component.enabled = true;
			component.applyRootMotion = false;
			component.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			component.fireEvents = false;
			_graph = PlayableGraph.Create(base.name + ".ImportedObstacleAnimation");
			_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
			_playable = AnimationClipPlayable.Create(_graph, _clip);
			_playable.SetApplyFootIK(value: false);
			AnimationPlayableOutput.Create(_graph, "Obstacle", component).SetSourcePlayable(_playable);
			_graph.Play();
			_graphClip = _clip;
			return true;
		}

		private void DestroyGraph()
		{
			if (_graph.IsValid())
			{
				_graph.Destroy();
			}
			_graphClip = null;
		}
	}
}
