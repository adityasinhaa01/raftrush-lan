using EPOOutline;
using PuffPirates.Boats;
using PuffPirates.Players;
using UnityEngine;

namespace PuffPirates.Rendering
{
	[RequireComponent(typeof(Outliner))]
	public class CrewXrayOutline : MonoBehaviour
	{
		private PiratePlayer _pirate;

		private NetworkBoat _boat;

		private Outlinable _charOl;

		private Outlinable _boatOl;

		private float _nextResolve;

		private void LateUpdate()
		{
			if (!(Time.unscaledTime < _nextResolve))
			{
				_nextResolve = Time.unscaledTime + 0.5f;
				Resolve();
			}
		}

		private void Resolve()
		{
			if (_pirate == null)
			{
				_pirate = PiratePlayer.Local;
			}
			if (_pirate == null)
			{
				return;
			}
			if (_charOl == null)
			{
				_charOl = _pirate.GetComponent<Outlinable>();
				if (_charOl != null)
				{
					_charOl.enabled = true;
				}
			}
			NetworkBoat boat = _pirate.Boat;
			if (boat != _boat)
			{
				if (_boatOl != null)
				{
					_boatOl.enabled = false;
				}
				_boat = boat;
				_boatOl = ((boat != null) ? boat.GetComponent<Outlinable>() : null);
				if (_boatOl != null)
				{
					_boatOl.enabled = true;
				}
			}
		}
	}
}
