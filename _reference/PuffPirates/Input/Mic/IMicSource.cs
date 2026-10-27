namespace PuffPirates.Input.Mic
{
	public interface IMicSource
	{
		bool Available { get; }

		void Start();

		void Stop();

		bool TryReadWindow(float[] dst, out int sampleRate);
	}
}
