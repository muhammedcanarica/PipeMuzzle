namespace PipeMuzzle.Gameplay
{
    public sealed class WorldProgressService
    {
        public bool IsWorldUnlocked(int worldIndex)
        {
            return worldIndex == 0;
        }
    }
}
