using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Save;
using GamesMrkt.Utils;

namespace PuffPirates.Save
{
	[RequireSystem(typeof(ISaveSystem))]
	public sealed class HostRunSystem : IGameSystem, IQuittableSystem, IPausableSystem
	{
		public const int MAX_RUNS = 5;

		private readonly ISaveSystem _save;

		private HostRunsData _data = new HostRunsData();

		private HostRunData _activeRun;

		private bool _isActiveRunStored;

		private bool _hasWarnedLimit;

		public string SystemId => "HostRunSystem";

		public IReadOnlyList<HostRunData> Runs => _data.runs;

		public bool CanStartNew => _data.runs.Count < 5;

		public event Action OnRunsChanged;

		public HostRunSystem(ISaveSystem save)
		{
			_save = save;
		}

		public async UniTask Initialize()
		{
			_data = (await _save.LoadAsync<HostRunsData>("host_runs")) ?? new HostRunsData();
			HostRunsData data = _data;
			if (data.runs == null)
			{
				data.runs = new List<HostRunData>();
			}
			_data.runs.RemoveAll((HostRunData run) => run == null || string.IsNullOrEmpty(run.id));
			SortRuns();
		}

		public void StartNew()
		{
			_activeRun = new HostRunData
			{
				id = Guid.NewGuid().ToString("N"),
				createdAt = NowMilliseconds()
			};
			_isActiveRunStored = false;
			_hasWarnedLimit = false;
		}

		public bool Continue(string runId)
		{
			HostRunData hostRunData = Find(runId);
			if (hostRunData == null)
			{
				return false;
			}
			_activeRun = hostRunData;
			_isActiveRunStored = true;
			return true;
		}

		public void Delete(string runId)
		{
			if (_activeRun != null && _activeRun.id == runId)
			{
				ReleaseActiveRun();
			}
			if (_data.runs.RemoveAll((HostRunData run) => run.id == runId) != 0)
			{
				Persist();
				RaiseRunsChanged();
			}
		}

		public HostRunData GetOrStartActiveRun()
		{
			if (_activeRun == null)
			{
				StartNew();
			}
			return _activeRun;
		}

		public int GetCheckpoint(int boatIndex)
		{
			return ValueAt(GetOrStartActiveRun().checkpoints, boatIndex);
		}

		public int GetDeaths(int boatIndex)
		{
			return ValueAt(GetOrStartActiveRun().deaths, boatIndex);
		}

		public void TrackElapsed(float seconds)
		{
			if (_activeRun != null)
			{
				_activeRun.elapsedSeconds = seconds;
			}
		}

		public void SetMap(int mapId)
		{
			GetOrStartActiveRun().mapId = mapId;
		}

		public void RecordCheckpoint(int boatIndex, int checkpoint)
		{
			SetAt(ref GetOrStartActiveRun().checkpoints, boatIndex, checkpoint);
			StoreActiveRun();
		}

		public void RecordDeaths(int boatIndex, int deaths)
		{
			SetAt(ref GetOrStartActiveRun().deaths, boatIndex, deaths);
			StoreActiveRun();
		}

		public void SaveActiveRun()
		{
			if (_activeRun != null && _isActiveRunStored)
			{
				StoreActiveRun();
			}
		}

		public void CompleteActiveRun()
		{
			if (_activeRun != null)
			{
				bool num = _isActiveRunStored && _data.runs.Remove(_activeRun);
				_activeRun = null;
				_isActiveRunStored = false;
				if (num)
				{
					Persist();
					RaiseRunsChanged();
				}
			}
		}

		public void ReleaseActiveRun()
		{
			SaveActiveRun();
			_activeRun = null;
			_isActiveRunStored = false;
		}

		public void OnApplicationQuitting()
		{
			SaveActiveRun();
		}

		public void OnPause()
		{
			SaveActiveRun();
		}

		public void OnResume()
		{
		}

		private void StoreActiveRun()
		{
			if (!_isActiveRunStored)
			{
				if (!CanStartNew)
				{
					if (!_hasWarnedLimit)
					{
						GameLogger.Warning($"Run limit ({5}) reached — this run is not saved.", SystemId);
					}
					_hasWarnedLimit = true;
					return;
				}
				_data.runs.Add(_activeRun);
				_isActiveRunStored = true;
			}
			_activeRun.updatedAt = NowMilliseconds();
			SortRuns();
			Persist();
			RaiseRunsChanged();
		}

		private void Persist()
		{
			_save.SaveAsync("host_runs", _data).Forget();
		}

		private void RaiseRunsChanged()
		{
			OnRunsChanged?.Invoke();
		}

		private HostRunData Find(string runId)
		{
			return _data.runs.Find((HostRunData run) => run.id == runId);
		}

		private void SortRuns()
		{
			_data.runs.Sort((HostRunData first, HostRunData second) => second.updatedAt.CompareTo(first.updatedAt));
		}

		private static long NowMilliseconds()
		{
			return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}

		private static int ValueAt(int[] values, int index)
		{
			if (values == null || index < 0 || index >= values.Length)
			{
				return 0;
			}
			return values[index];
		}

		private static void SetAt(ref int[] values, int index, int value)
		{
			if (index >= 0)
			{
				if (values == null)
				{
					values = new int[0];
				}
				if (index >= values.Length)
				{
					Array.Resize(ref values, index + 1);
				}
				values[index] = value;
			}
		}
	}
}
