using NUnit.Framework;
using PipeMuzzle.Gameplay;

namespace PipeMuzzle.Tests.EditMode
{
    public class WorldProgressServiceTests
    {
        [Test]
        public void OnlySakuraIsUnlockedAtFirstLaunch()
        {
            WorldProgressService progress = new WorldProgressService();

            Assert.That(progress.IsWorldUnlocked(0), Is.True);
            Assert.That(progress.IsWorldUnlocked(1), Is.False);
            Assert.That(progress.IsWorldUnlocked(2), Is.False);
        }
    }
}
