using PuffPirates.Boats;
using UnityEngine;
using UnityEngine.Playables;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(Animator))]
	public class TriggeredRoad : MonoBehaviour
	{
		[SerializeField]
		private AnimationClip _clip;

		[Tooltip("Clip'in yollarının göreceli olduğu Animator (FBX modelin kökü). Boşsa bu objedeki Animator.")]
		[SerializeField]
		private Animator _animator;

		[SerializeField]
		private Collider _trigger;

		private PlayableGraph _graph;

		private bool _played;

		private void OnEnable()
		{
			_played = false;
		}

		private void OnDisable()
		{
			if (_graph.IsValid())
			{
				_graph.Destroy();
			}
		}

		private void Update()
		{
			if (_played || _clip == null || _trigger == null)
			{
				return;
			}
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (!(item == null) && !((_trigger.ClosestPoint(item.transform.position) - item.transform.position).sqrMagnitude > 1E-06f))
				{
					_played = true;
					AnimationPlayableUtilities.PlayClip((_animator != null) ? _animator : GetComponent<Animator>(), _clip, out _graph);
					break;
				}
			}
		}
	}
}
