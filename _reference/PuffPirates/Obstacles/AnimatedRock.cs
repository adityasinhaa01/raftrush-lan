using PuffPirates.Boats;
using UnityEngine;
using UnityEngine.Playables;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(Animator))]
	public class AnimatedRock : MonoBehaviour
	{
		[SerializeField]
		private AnimationClip _clip;

		[Tooltip("Clip'in yollarının göreceli olduğu Animator (FBX modelin kökü). Boşsa bu objedeki Animator.")]
		[SerializeField]
		private Animator _animator;

		[Tooltip("Başlatan alan (opsiyonel). Boşsa animasyon obje açılınca başlar.")]
		[SerializeField]
		private Collider _startTrigger;

		[Tooltip("Animasyon başlayana kadar gizli olan taş gövdesi (mesh + collider).")]
		[SerializeField]
		private GameObject _body;

		private PlayableGraph _graph;

		private float _endTime;

		private bool _playing;

		private void OnEnable()
		{
			_playing = false;
			if (_startTrigger == null)
			{
				Play();
			}
			else if (_body != null)
			{
				_body.SetActive(value: false);
			}
		}

		private void OnDisable()
		{
			if (_graph.IsValid())
			{
				_graph.Destroy();
			}
		}

		private void Play()
		{
			if (_clip == null)
			{
				base.gameObject.SetActive(value: false);
				return;
			}
			if (_body != null)
			{
				_body.SetActive(value: true);
			}
			AnimationPlayableUtilities.PlayClip((_animator != null) ? _animator : GetComponent<Animator>(), _clip, out _graph);
			_endTime = Time.time + _clip.length;
			_playing = true;
		}

		private void Update()
		{
			if (_playing)
			{
				if (Time.time >= _endTime)
				{
					base.gameObject.SetActive(value: false);
				}
				return;
			}
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (item != null && (_startTrigger.ClosestPoint(item.transform.position) - item.transform.position).sqrMagnitude < 1E-06f)
				{
					Play();
					break;
				}
			}
		}
	}
}
