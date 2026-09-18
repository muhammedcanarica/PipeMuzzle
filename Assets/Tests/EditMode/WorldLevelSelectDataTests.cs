using NUnit.Framework;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class WorldLevelSelectDataTests
    {
        [Test]
        public void LevelPathRequiresExactlyTwelveNodes()
        {
            LevelPathLayoutDefinition layout = ScriptableObject.CreateInstance<LevelPathLayoutDefinition>();

            Assert.That(layout.HasValidNodeCount, Is.True);
            Assert.That(layout.NodeCount, Is.EqualTo(12));

            Object.DestroyImmediate(layout);
        }
    }
}
