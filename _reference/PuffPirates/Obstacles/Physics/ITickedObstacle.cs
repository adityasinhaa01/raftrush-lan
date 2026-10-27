namespace PuffPirates.Obstacles.Physics
{
	public interface ITickedObstacle
	{
		void ObstacleTick(int tick, float dt, float time);
	}
}
