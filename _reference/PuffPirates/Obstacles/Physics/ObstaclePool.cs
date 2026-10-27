using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class ObstaclePool<T> where T : Component
	{
		private readonly T _prefab;

		private readonly Transform _parent;

		private readonly int _maxCap;

		private readonly Stack<T> _free = new Stack<T>();

		private int _liveCount;

		private bool _warnedAtCap;

		public ObstaclePool(T prefab, Transform parent, int prewarmCount, int maxCap)
		{
			_prefab = prefab;
			_parent = parent;
			_maxCap = maxCap;
			if (_prefab == null)
			{
				Debug.LogWarning("[ObstaclePool<" + typeof(T).Name + ">] Prefab null — havuz devre dışı, tüm Get() null döner.");
			}
			else
			{
				Prewarm(prewarmCount);
			}
		}

		public void Prewarm(int n)
		{
			if (_prefab == null)
			{
				return;
			}
			for (int i = 0; i < n; i++)
			{
				if (_maxCap > 0 && _liveCount >= _maxCap)
				{
					break;
				}
				T val = CreateInstance();
				if (!(val == null))
				{
					val.gameObject.SetActive(value: false);
					_free.Push(val);
					continue;
				}
				break;
			}
		}

		public T Get(Vector3 pos, Quaternion rot)
		{
			if (_prefab == null)
			{
				return null;
			}
			T val = null;
			while (_free.Count > 0)
			{
				T val2 = _free.Pop();
				if (val2 != null)
				{
					val = val2;
					break;
				}
				_liveCount--;
			}
			if (val == null)
			{
				if (_maxCap > 0 && _liveCount >= _maxCap)
				{
					if (!_warnedAtCap)
					{
						_warnedAtCap = true;
						Debug.LogWarning($"[ObstaclePool<{typeof(T).Name}>] Havuz kapasitesi doldu ({_maxCap}) — Get() null döndü.");
					}
					return null;
				}
				val = CreateInstance();
				if (val == null)
				{
					return null;
				}
			}
			val.transform.SetPositionAndRotation(pos, rot);
			val.gameObject.SetActive(value: true);
			return val;
		}

		public void Return(T instance)
		{
			if (!(instance == null) && instance.gameObject.activeSelf)
			{
				instance.gameObject.SetActive(value: false);
				_free.Push(instance);
			}
		}

		private T CreateInstance()
		{
			T result = Object.Instantiate(_prefab, _parent);
			_liveCount++;
			return result;
		}
	}
}
