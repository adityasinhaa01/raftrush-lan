using System.Collections.Generic;
using EPOOutline;
using PuffPirates.Players;
using UnityEngine;

namespace PuffPirates.Rendering
{
	[RequireComponent(typeof(Outliner))]
	public class CrewMaskOutline : MonoBehaviour
	{
		private PiratePlayer _localPirate;

		private readonly Dictionary<PiratePlayer, Outlinable> _masked = new Dictionary<PiratePlayer, Outlinable>();

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
			if (_localPirate == null)
			{
				_localPirate = PiratePlayer.Local;
			}
			PiratePlayer[] array = Object.FindObjectsByType<PiratePlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (PiratePlayer piratePlayer in array)
			{
				if (piratePlayer == _localPirate)
				{
					continue;
				}
				if (!_masked.TryGetValue(piratePlayer, out var value) || value == null)
				{
					value = piratePlayer.GetComponent<Outlinable>();
					if (value == null)
					{
						continue;
					}
					_masked[piratePlayer] = value;
				}
				if (!value.enabled)
				{
					value.enabled = true;
				}
				OutlinableDrawingMode outlinableDrawingMode = ((_localPirate != null && _localPirate.Boat != null && piratePlayer.Boat == _localPirate.Boat) ? (OutlinableDrawingMode.Normal | OutlinableDrawingMode.Mask) : OutlinableDrawingMode.Mask);
				if (value.DrawingMode != outlinableDrawingMode)
				{
					value.DrawingMode = outlinableDrawingMode;
				}
			}
		}
	}
}
