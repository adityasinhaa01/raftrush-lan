using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.Networking;
using PuffPirates.Players;
using UnityEngine;
using UnityEngine.VFX;

namespace PuffPirates.Juice
{
	[DisallowMultipleComponent]
	public class RaceStartReveal : MonoBehaviour
	{
		[Tooltip("KAPALI (varsayılan): SpawnCloud prefabı ToonExplosionEmbers içerdiğinden geri sayım başında her botta + korsanda PATLAMA gibi görünüyordu (2 bot = 2 patlama, 'oyuna girer girmez patlama' hatası). Açılırsa bot/korsanlar geri sayımda gizlenip bulutla gösterilir.")]
		[SerializeField]
		private bool _useCloudFx;

		[Tooltip("Spawn bulut/toz efekti (AllIn1 Toon Explosion). Tek-atım; _cloudLife sonra yok edilir.")]
		[SerializeField]
		private GameObject _cloudFx;

		[Tooltip("Bulut efektinin ömrü (sn) — sonra Destroy.")]
		[SerializeField]
		private float _cloudLife = 2.5f;

		[Tooltip("Bulut ölçeği.")]
		[SerializeField]
		private float _cloudScale = 1.6f;

		[Tooltip("Botların üstünde bulut yüksekliği (m).")]
		[SerializeField]
		private float _cloudYOffset = 0.5f;

		private const float PirateRevealDelay = 1f;

		private bool _counting;

		private float _total = 3f;

		private int _phase;

		private readonly List<Renderer> _boatR = new List<Renderer>();

		private readonly List<Renderer> _pirateR = new List<Renderer>();

		private static RaceStartReveal _instance;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (!(_instance != null))
			{
				new GameObject("~RaceStartReveal").AddComponent<RaceStartReveal>();
			}
		}

		private void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			_instance = this;
			if (base.transform.parent == null)
			{
				Object.DontDestroyOnLoad(base.gameObject);
			}
			if (_cloudFx == null)
			{
				_cloudFx = Resources.Load<GameObject>("Fx/SpawnCloud");
			}
			base.enabled = _useCloudFx;
		}

		private void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
			}
		}

		private void Update()
		{
			if (!_useCloudFx)
			{
				return;
			}
			RaceState instance = RaceState.Instance;
			if (instance == null)
			{
				if (_counting || _boatR.Count > 0 || _pirateR.Count > 0)
				{
					ForceShowAll();
				}
				return;
			}
			float countdownRemaining = instance.CountdownRemaining;
			bool flag = instance.InLobby && countdownRemaining > 0.01f;
			if (flag && !_counting)
			{
				BeginReveal(countdownRemaining);
			}
			else if (!flag && _counting)
			{
				ForceShowAll();
			}
			if (_counting && _phase == 3 && countdownRemaining <= _total - 1f)
			{
				RevealPirates();
				_phase = 2;
			}
		}

		private void BeginReveal(float rem)
		{
			_counting = true;
			_total = Mathf.Max(rem, 1f);
			_phase = 3;
			CollectRenderers();
			Show(_boatR, visible: false);
			Show(_pirateR, visible: false);
			List<NetworkBoat> all = NetworkBoat.All;
			for (int i = 0; i < all.Count; i++)
			{
				if (all[i] != null)
				{
					SpawnCloud(all[i].transform.position);
				}
			}
			Show(_boatR, visible: true);
		}

		private void RevealPirates()
		{
			PiratePlayer[] array = Object.FindObjectsByType<PiratePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (PiratePlayer piratePlayer in array)
			{
				if (piratePlayer != null)
				{
					SpawnCloud(piratePlayer.transform.position + Vector3.up * 0.2f);
				}
			}
			Show(_pirateR, visible: true);
		}

		private void ForceShowAll()
		{
			Show(_boatR, visible: true);
			Show(_pirateR, visible: true);
			_boatR.Clear();
			_pirateR.Clear();
			_counting = false;
			_phase = 0;
		}

		private void CollectRenderers()
		{
			_boatR.Clear();
			_pirateR.Clear();
			List<NetworkBoat> all = NetworkBoat.All;
			for (int i = 0; i < all.Count; i++)
			{
				NetworkBoat networkBoat = all[i];
				if (networkBoat == null)
				{
					continue;
				}
				Renderer[] componentsInChildren = networkBoat.GetComponentsInChildren<Renderer>(includeInactive: true);
				foreach (Renderer renderer in componentsInChildren)
				{
					if (IsBodyRenderer(renderer) && !(renderer.GetComponentInParent<PiratePlayer>() != null))
					{
						_boatR.Add(renderer);
					}
				}
			}
			PiratePlayer[] array = Object.FindObjectsByType<PiratePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (PiratePlayer piratePlayer in array)
			{
				if (piratePlayer == null)
				{
					continue;
				}
				Renderer[] componentsInChildren = piratePlayer.GetComponentsInChildren<Renderer>(includeInactive: true);
				foreach (Renderer renderer2 in componentsInChildren)
				{
					if (IsBodyRenderer(renderer2))
					{
						_pirateR.Add(renderer2);
					}
				}
			}
		}

		private static bool IsBodyRenderer(Renderer r)
		{
			if (r == null)
			{
				return false;
			}
			if (r is ParticleSystemRenderer || r is LineRenderer || r is VFXRenderer)
			{
				return false;
			}
			if (r.transform.name.StartsWith("~"))
			{
				return false;
			}
			if (!r.enabled)
			{
				return false;
			}
			if (!(r is MeshRenderer))
			{
				return r is SkinnedMeshRenderer;
			}
			return true;
		}

		private static void Show(List<Renderer> list, bool visible)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != null)
				{
					list[i].enabled = visible;
				}
			}
		}

		private void SpawnCloud(Vector3 pos)
		{
			if (!(_cloudFx == null))
			{
				GameObject obj = Object.Instantiate(_cloudFx, pos + Vector3.up * _cloudYOffset, Quaternion.identity);
				obj.transform.localScale *= _cloudScale;
				Object.Destroy(obj, _cloudLife);
			}
		}
	}
}
