using System;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Localization;
using PuffPirates.Biomes;
using PuffPirates.Networking;
using PuffPirates.Utils;
using UnityEngine;

namespace PuffPirates.Systems
{
	public class MatchConfigSystem : IGameSystem, IUpdateableSystem
	{
		private float _matchDurationSec = 180f;

		private float _lastTurn = float.NaN;

		private float _lastNormalGas = float.NaN;

		private float _lastSwirl = float.NaN;

		private int _lastSector = -1;

		private int _lastCrewBlocking = -1;

		private int _lastRotX = -1;

		private int _lastRotY = -1;

		public string SystemId => "MatchConfigSystem";

		private static bool CanWrite
		{
			get
			{
				if (RaceState.Instance != null)
				{
					return RaceState.Instance.IsServerInitialized;
				}
				return false;
			}
		}

		public float MatchDuration => _matchDurationSec;

		public float TurnSpeedDeg
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 140f;
				}
				return RaceState.Instance.TurnSpeedDeg;
			}
		}

		public float NormalGasForce
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 235f;
				}
				return RaceState.Instance.NormalGasForce;
			}
		}

		public float SwirlBias
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 0.01f;
				}
				return RaceState.Instance.SwirlBias;
			}
		}

		public bool CrewBlocking
		{
			get
			{
				if (RaceState.Instance != null)
				{
					return RaceState.Instance.CrewBlocking;
				}
				return false;
			}
		}

		public bool SectorLock
		{
			get
			{
				if (!(RaceState.Instance == null))
				{
					return RaceState.Instance.SectorLock;
				}
				return true;
			}
		}

		public bool LockRotX
		{
			get
			{
				if (!(RaceState.Instance == null))
				{
					return RaceState.Instance.LockRotX;
				}
				return true;
			}
		}

		public bool LockRotY
		{
			get
			{
				if (!(RaceState.Instance == null))
				{
					return RaceState.Instance.LockRotY;
				}
				return true;
			}
		}

		public bool BoundaryLeanEnabled
		{
			get
			{
				if (RaceState.Instance != null)
				{
					return RaceState.Instance.BoundaryLeanEnabled;
				}
				return false;
			}
		}

		public event Action Changed;

		public event Action<string> Toast;

		public UniTask Initialize()
		{
			return UniTask.CompletedTask;
		}

		public void OnUpdate(float deltaTime)
		{
			RaceState instance = RaceState.Instance;
			if (!(instance == null) && (!Mathf.Approximately(instance.TurnSpeedDeg, _lastTurn) || !Mathf.Approximately(instance.NormalGasForce, _lastNormalGas) || !Mathf.Approximately(instance.SwirlBias, _lastSwirl) || (instance.CrewBlocking ? 1 : 0) != _lastCrewBlocking || (instance.SectorLock ? 1 : 0) != _lastSector || (instance.LockRotX ? 1 : 0) != _lastRotX || (instance.LockRotY ? 1 : 0) != _lastRotY))
			{
				_lastTurn = instance.TurnSpeedDeg;
				_lastNormalGas = instance.NormalGasForce;
				_lastSwirl = instance.SwirlBias;
				_lastCrewBlocking = (instance.CrewBlocking ? 1 : 0);
				_lastSector = (instance.SectorLock ? 1 : 0);
				_lastRotX = (instance.LockRotX ? 1 : 0);
				_lastRotY = (instance.LockRotY ? 1 : 0);
				Changed?.Invoke();
			}
		}

		public void SetMatchDuration(int seconds)
		{
			if (CanWrite)
			{
				_matchDurationSec = seconds;
				Toast?.Invoke(Loc.Get("net.toast.duration_changed", ("duration", MatchTimeFormat.Format(seconds))));
				Changed?.Invoke();
			}
		}

		public void SetTheme(int id)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetThemeId(id);
				Toast?.Invoke("Tema: " + ((BiomeController.Instance != null) ? BiomeController.Instance.ThemeName(id) : "Default"));
			}
		}

		public void SetTurnSpeed(float degPerSec)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetTurnSpeed(degPerSec);
				Changed?.Invoke();
			}
		}

		public void SetGasForce(float normal)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetGasForce(normal);
				Changed?.Invoke();
			}
		}

		public void SetSwirlBias(float bias)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetSwirlBias(bias);
				Changed?.Invoke();
			}
		}

		public void SetSectorLock(bool on)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetSectorLock(on);
				Toast?.Invoke(on ? Loc.Get("net.toast.sector_lock_on") : Loc.Get("net.toast.sector_lock_off"));
			}
		}

		public void SetBoundaryLean(bool on)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetBoundaryLean(on);
			}
		}

		public void SetCrewBlocking(bool on)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetCrewBlocking(on);
				Toast?.Invoke(on ? Loc.Get("net.toast.crew_blocking_on") : Loc.Get("net.toast.crew_blocking_off"));
			}
		}

		public void SetRotationLock(bool lockX, bool lockY)
		{
			if (CanWrite)
			{
				RaceState.Instance.SetRotationLock(lockX, lockY);
				Toast?.Invoke((!lockX) ? (lockY ? Loc.Get("net.toast.rotation_lock_y_only") : Loc.Get("net.toast.rotation_lock_none")) : (lockY ? Loc.Get("net.toast.rotation_lock_xy") : Loc.Get("net.toast.rotation_lock_x_only")));
			}
		}
	}
}
