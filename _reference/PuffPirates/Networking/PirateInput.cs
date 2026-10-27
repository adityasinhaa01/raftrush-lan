using FishNet.Object.Prediction;

namespace PuffPirates.Networking
{
	public struct PirateInput : IReplicateData
	{
		public float CircularMovement;

		public bool NormalGas;

		public bool MousePuff;

		public bool SwitchBoat;

		public bool UseItem;

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
