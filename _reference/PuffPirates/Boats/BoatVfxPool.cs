using UnityEngine;

namespace PuffPirates.Boats
{
	internal sealed class BoatVfxPool
	{
		private const int MinCapacity = 1;

		private const float NoScale = 0f;

		private readonly GameObject _prefab;

		private readonly Vector3 _prefabScale;

		private readonly int _max;

		private readonly Transform _container;

		private readonly GameObject[] _go;

		private readonly ParticleSystem[][] _systems;

		private readonly float[] _returnAt;

		private readonly bool[] _active;

		private int _count;

		public BoatVfxPool(GameObject prefab, int max, string containerName)
		{
			_prefab = prefab;
			_prefabScale = ((prefab != null) ? prefab.transform.localScale : Vector3.one);
			_max = Mathf.Max(1, max);
			_go = new GameObject[_max];
			_systems = new ParticleSystem[_max][];
			_returnAt = new float[_max];
			_active = new bool[_max];
			_container = new GameObject(string.IsNullOrEmpty(containerName) ? "BoatVfxPool" : containerName).transform;
			_container.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
		}

		public void Spawn(Vector3 position, Quaternion rotation, float uniformScale, float lifetime)
		{
			if (_prefab == null)
			{
				return;
			}
			float time = Time.time;
			for (int i = 0; i < _count; i++)
			{
				if (!_active[i] && !(_go[i] == null))
				{
					Reactivate(i, position, rotation, uniformScale, time + lifetime);
					return;
				}
			}
			if (_count < _max)
			{
				int num = _count++;
				GameObject gameObject = Object.Instantiate(_prefab, position, rotation, _container);
				gameObject.transform.localScale = ResolveScale(uniformScale);
				_go[num] = gameObject;
				_systems[num] = gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
				_active[num] = true;
				_returnAt[num] = time + lifetime;
			}
			else
			{
				GameObject gameObject2 = Object.Instantiate(_prefab, position, rotation);
				gameObject2.transform.localScale = ResolveScale(uniformScale);
				Object.Destroy(gameObject2, lifetime);
			}
		}

		public void Tick()
		{
			float time = Time.time;
			for (int i = 0; i < _count; i++)
			{
				if (_active[i])
				{
					if (_go[i] == null)
					{
						_active[i] = false;
					}
					else if (time >= _returnAt[i])
					{
						_active[i] = false;
						_go[i].SetActive(value: false);
					}
				}
			}
		}

		public void Dispose()
		{
			for (int i = 0; i < _count; i++)
			{
				if (_go[i] != null)
				{
					Object.Destroy(_go[i]);
				}
			}
			if (_container != null)
			{
				Object.Destroy(_container.gameObject);
			}
			_count = 0;
		}

		private Vector3 ResolveScale(float uniformScale)
		{
			if (!(uniformScale > 0f))
			{
				return _prefabScale;
			}
			return Vector3.one * uniformScale;
		}

		private void Reactivate(int i, Vector3 position, Quaternion rotation, float uniformScale, float returnAt)
		{
			GameObject gameObject = _go[i];
			Transform transform = gameObject.transform;
			transform.SetPositionAndRotation(position, rotation);
			transform.localScale = ResolveScale(uniformScale);
			_active[i] = true;
			_returnAt[i] = returnAt;
			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(value: true);
			}
			ParticleSystem[] array = _systems[i];
			if (array == null)
			{
				return;
			}
			foreach (ParticleSystem particleSystem in array)
			{
				if (!(particleSystem == null))
				{
					particleSystem.Clear();
					particleSystem.Play();
				}
			}
		}
	}
}
