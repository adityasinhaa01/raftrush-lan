namespace PuffPirates.Boats
{
	public readonly struct BoatSizeDef
	{
		public readonly string Label;

		public readonly float Diameter;

		public readonly float Height;

		public readonly float Mass;

		public readonly float AttachRadius;

		public readonly float MaxSpeed;

		public BoatSizeDef(string label, float diameter, float height, float mass, float attachRadius, float maxSpeed)
		{
			Label = label;
			Diameter = diameter;
			Height = height;
			Mass = mass;
			AttachRadius = attachRadius;
			MaxSpeed = maxSpeed;
		}
	}
}
