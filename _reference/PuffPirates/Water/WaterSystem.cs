using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Water
{
	public static class WaterSystem
	{
		private static readonly List<WaterChannel> _channels = new List<WaterChannel>();

		private static readonly Dictionary<int, int> _defaultLayerIgnoreRefs = new Dictionary<int, int>();

		private static readonly Dictionary<int, bool> _defaultLayerOriginalStates = new Dictionary<int, bool>();

		public static IReadOnlyList<WaterChannel> Channels => _channels;

		public static void Register(WaterChannel c)
		{
			if (c != null && !_channels.Contains(c))
			{
				_channels.Add(c);
			}
		}

		public static void Unregister(WaterChannel c)
		{
			_channels.Remove(c);
		}

		internal static void AcquireDefaultLayerIgnore(int layer)
		{
			if (layer <= 0)
			{
				return;
			}
			if (_defaultLayerIgnoreRefs.TryGetValue(layer, out var value))
			{
				_defaultLayerIgnoreRefs[layer] = value + 1;
				return;
			}
			bool ignoreLayerCollision = Physics.GetIgnoreLayerCollision(layer, 0);
			_defaultLayerOriginalStates[layer] = ignoreLayerCollision;
			_defaultLayerIgnoreRefs[layer] = 1;
			if (!ignoreLayerCollision)
			{
				Physics.IgnoreLayerCollision(layer, 0, ignore: true);
			}
		}

		internal static void ReleaseDefaultLayerIgnore(int layer)
		{
			if (layer > 0 && _defaultLayerIgnoreRefs.TryGetValue(layer, out var value))
			{
				if (value > 1)
				{
					_defaultLayerIgnoreRefs[layer] = value - 1;
					return;
				}
				bool ignore = _defaultLayerOriginalStates.TryGetValue(layer, out var value2) & value2;
				Physics.IgnoreLayerCollision(layer, 0, ignore);
				_defaultLayerIgnoreRefs.Remove(layer);
				_defaultLayerOriginalStates.Remove(layer);
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetRegistry()
		{
			foreach (KeyValuePair<int, bool> defaultLayerOriginalState in _defaultLayerOriginalStates)
			{
				Physics.IgnoreLayerCollision(defaultLayerOriginalState.Key, 0, defaultLayerOriginalState.Value);
			}
			_defaultLayerIgnoreRefs.Clear();
			_defaultLayerOriginalStates.Clear();
			_channels.Clear();
		}

		public static bool TrySampleWater(Vector3 worldPos, out float waterY, out WaterChannel channel)
		{
			waterY = 0f;
			channel = null;
			float num = float.MaxValue;
			for (int i = 0; i < _channels.Count; i++)
			{
				WaterChannel waterChannel = _channels[i];
				if (!(waterChannel == null) && waterChannel.Contains(worldPos, out var surfaceY))
				{
					float num2 = Mathf.Abs(worldPos.y - surfaceY);
					if (num2 < num)
					{
						num = num2;
						waterY = surfaceY;
						channel = waterChannel;
					}
				}
			}
			return channel != null;
		}
	}
}
