using System;
using Cysharp.Threading.Tasks;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using GamesMrkt.Audio;
using PuffPirates.Audio;
using UnityEngine;

namespace GamesMrkt.PuffPirates.Ground
{
	[DisallowMultipleComponent]
	public class GroundSoundHandler : NetworkBehaviour
	{
		[Serializable]
		private struct GroundSoundEntry
		{
			public GroundType GroundType;

			public SoundData Sound;
		}

		[SerializeField]
		private GroundSoundEntry[] _sounds = Array.Empty<GroundSoundEntry>();

		[Tooltip("Used when the hit IGround has no entry of its own. Leave empty to stay silent.")]
		[SerializeField]
		private SoundData _fallbackSound;

		[Tooltip("Relative speed (m/s) below which a contact makes no sound.")]
		[SerializeField]
		[Min(0f)]
		private float _minImpactSpeed = 1.1f;

		[Tooltip("Relative speed (m/s) at which the sound reaches full volume.")]
		[SerializeField]
		[Min(0.01f)]
		private float _maxImpactSpeed = 7f;

		[SerializeField]
		[Range(0f, 1f)]
		private float _minVolume = 0.5f;

		[Tooltip("Minimum seconds between two sounds from this body, to avoid spam on scraping contacts.")]
		[SerializeField]
		[Min(0f)]
		private float _cooldown = 0.12f;

		private float _nextAllowedTime;

		private bool NetworkInitialize___EarlyGamesMrkt_002EPuffPirates_002EGround_002EGroundSoundHandlerAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LateGamesMrkt_002EPuffPirates_002EGround_002EGroundSoundHandlerAssembly_002DCSharp_002Edll_Excuted;

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			GroundSoundEntry[] sounds = _sounds;
			for (int i = 0; i < sounds.Length; i++)
			{
				GroundSoundEntry groundSoundEntry = sounds[i];
				if (groundSoundEntry.Sound != null)
				{
					AudioManager.PreloadAsync(groundSoundEntry.Sound).Forget();
				}
			}
			if (_fallbackSound != null)
			{
				AudioManager.PreloadAsync(_fallbackSound).Forget();
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (!base.IsServerInitialized || collision == null || Time.time < _nextAllowedTime)
			{
				return;
			}
			IGround componentInParent = collision.collider.GetComponentInParent<IGround>();
			if (componentInParent == null)
			{
				return;
			}
			float magnitude = collision.relativeVelocity.magnitude;
			if (magnitude < _minImpactSpeed)
			{
				return;
			}
			int contactCount = collision.contactCount;
			if (contactCount != 0)
			{
				Vector3 zero = Vector3.zero;
				for (int i = 0; i < contactCount; i++)
				{
					zero += collision.GetContact(i).point;
				}
				zero /= (float)contactCount;
				float volume = Mathf.Lerp(_minVolume, 1f, Mathf.InverseLerp(_minImpactSpeed, _maxImpactSpeed, magnitude));
				_nextAllowedTime = Time.time + _cooldown;
				RpcPlayGroundSound(componentInParent.GroundType, zero, volume);
			}
		}

		[ObserversRpc]
		private void RpcPlayGroundSound(GroundType groundType, Vector3 point, float volume, FishNet.Transporting.Channel channel = FishNet.Transporting.Channel.Unreliable)
		{
			RpcWriter___Observers_RpcPlayGroundSound___2087832800(groundType, point, volume, channel);
		}

		private SoundData FindSound(GroundType groundType)
		{
			GroundSoundEntry[] sounds = _sounds;
			for (int i = 0; i < sounds.Length; i++)
			{
				GroundSoundEntry groundSoundEntry = sounds[i];
				if (groundSoundEntry.GroundType == groundType && groundSoundEntry.Sound != null)
				{
					return groundSoundEntry.Sound;
				}
			}
			return _fallbackSound;
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyGamesMrkt_002EPuffPirates_002EGround_002EGroundSoundHandlerAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyGamesMrkt_002EPuffPirates_002EGround_002EGroundSoundHandlerAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				RegisterObserversRpc(0u, RpcReader___Observers_RpcPlayGroundSound___2087832800);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LateGamesMrkt_002EPuffPirates_002EGround_002EGroundSoundHandlerAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LateGamesMrkt_002EPuffPirates_002EGround_002EGroundSoundHandlerAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void RpcWriter___Observers_RpcPlayGroundSound___2087832800(GroundType groundType, Vector3 point, float volume, FishNet.Transporting.Channel channel = FishNet.Transporting.Channel.Unreliable)
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			FishNet.Transporting.Channel channel2 = channel;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			GeneratedWriters___Internal.GWrite___GamesMrkt_002EPuffPirates_002EGround_002EGroundTypeFishNet_002ESerializing_002EGenerated(pooledWriter, groundType);
			pooledWriter.WriteVector3(point);
			pooledWriter.WriteSingle(volume);
			SendObserversRpc(0u, pooledWriter, channel2, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			pooledWriter.Store();
		}

		private void RpcLogic___RpcPlayGroundSound___2087832800(GroundType P_0, Vector3 P_1, float P_2, FishNet.Transporting.Channel P_3)
		{
			SoundData soundData = FindSound(P_0);
			if (soundData != null)
			{
				Sfx3D.Play(soundData, P_1, P_2);
			}
		}

		private void RpcReader___Observers_RpcPlayGroundSound___2087832800(PooledReader PooledReader0, FishNet.Transporting.Channel channel)
		{
			GroundType groundType = GeneratedReaders___Internal.GRead___GamesMrkt_002EPuffPirates_002EGround_002EGroundTypeFishNet_002ESerializing_002EGenerateds(PooledReader0);
			Vector3 vector = PooledReader0.ReadVector3();
			float num = PooledReader0.ReadSingle();
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcPlayGroundSound___2087832800(groundType, vector, num, channel);
			}
		}

		public virtual void Awake()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}
	}
}
