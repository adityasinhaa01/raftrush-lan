using FishNet.Object.Prediction;

namespace PuffPirates.Networking
{
	public struct PirateSeatState : IReconcileData
	{
		public float Angle;

		private uint _tick;

		public void Dispose()
		{
		}

		public uint GetTick()
		{
			return _tick;
		}

		public void SetTick(uint value)
		{
			_tick = value;
		}
	}
}
