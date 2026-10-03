using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class PipeChannelVisualTests
    {
        private static readonly string[] Worlds = { "SakuraGarden", "BambooWorkshop", "MoonShrine" };
        private static IEnumerable<TestCaseData> Rotations()
        {
            foreach (string world in Worlds)
            foreach (TileShape shape in new[] { TileShape.Straight, TileShape.Corner, TileShape.ThreeWay, TileShape.Cross })
            for (int rotation = 0; rotation < 4; rotation++)
                yield return new TestCaseData(world, shape, rotation);
        }

        [TestCaseSource(nameof(Rotations))]
        public void ArtworkOpeningsMatchLogicalConnectionsAtEveryRotation(string world, TileShape shape, int rotation)
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab"));
            Texture2D pixels = new(2, 2);
            try
            {
                TileView tile = instance.GetComponent<TileView>();
                tile.ApplyTheme(Theme(world));
                TileState state = new(0, 0, shape, TileRole.Normal, rotation, false);
                tile.Initialize(state);
                Sprite sprite = instance.GetComponent<SpriteRenderer>().sprite;
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(410));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(313.5f, 313.5f)));
                pixels.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                Vector3[] directions = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
                ConnectionMask[] masks = { ConnectionMask.North, ConnectionMask.East, ConnectionMask.South, ConnectionMask.West };
                for (int side = 0; side < 4; side++)
                {
                    // Sample the actual rotated artwork at the cell seam, independently of its sprite orientation rules.
                    Vector3 local = Quaternion.Inverse(tile.transform.localRotation) * (directions[side] * .493f);
                    int x = Mathf.RoundToInt(313 + local.x * sprite.pixelsPerUnit);
                    int y = Mathf.RoundToInt(313 + local.y * sprite.pixelsPerUnit);
                    float alpha = pixels.GetPixel(x, y).a;
                    bool connected = (state.Connections & masks[side]) != 0;
                    Assert.That(alpha, connected ? Is.GreaterThan(.95f) : Is.LessThan(.01f), masks[side].ToString());
                }
            }
            finally { Object.DestroyImmediate(pixels); Object.DestroyImmediate(instance); }
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void SourceAndTargetUseDistinctUprightMotifs(string world)
        {
            WorldGameplayTheme theme = Theme(world);
            Assert.That(theme.SourceMarker, Is.Not.Null);
            Assert.That(theme.TargetMarker, Is.Not.Null.And.Not.SameAs(theme.SourceMarker));
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab"));
            try
            {
                TileView tile = instance.GetComponent<TileView>();
                tile.ApplyTheme(theme);
                foreach (TileRole role in new[] { TileRole.Source, TileRole.Target })
                {
                    tile.Initialize(new TileState(0, 0, TileShape.Corner, role, 3, true));
                    SpriteRenderer marker = instance.transform.Find("RoleVisual").GetComponent<SpriteRenderer>();
                    Assert.That(marker.sprite, Is.SameAs(role == TileRole.Source ? theme.SourceMarker : theme.TargetMarker));
                    Assert.That(Quaternion.Angle(marker.transform.rotation, Quaternion.identity), Is.LessThan(.01f));
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void FlowFillsOnlyTraversedSegmentsThenCompletesAndResets(string world)
        {
            GameObject root = new("FlowTest");
            GameObject targetObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab"));
            try
            {
                TileView target = targetObject.GetComponent<TileView>();
                target.Initialize(new TileState(1, 1, TileShape.Corner, TileRole.Target, 0, true));
                EnergyFlowView flow = root.AddComponent<EnergyFlowView>();
                flow.Configure(Theme(world));
                int completions = 0;
                Vector3[] path = { Vector3.zero, Vector3.right, Vector3.right + Vector3.up };
                Assert.That(flow.Play(path, target, () => completions++), Is.True);
                Assert.That(flow.Duration, Is.InRange(.5f, 1.2f));
                LineRenderer fill = root.GetComponent<LineRenderer>();
                Assert.That(fill.startWidth, Is.GreaterThanOrEqualTo(.17f), "Fill must occupy the channel, rather than a thin tracer.");
                flow.Advance(.125f);
                Assert.That(fill.GetPosition(fill.positionCount - 1), Is.EqualTo(new Vector3(.5f, 0, 0)));
                Assert.That(fill.startColor, Is.EqualTo(Theme(world).FlowColor));
                Assert.That(completions, Is.Zero);
                flow.Advance(.375f);
                Assert.That(fill.GetPosition(fill.positionCount - 1), Is.EqualTo(path[2]));
                Assert.That(completions, Is.Zero, "Arrival must be visible before the result event.");
                flow.Advance(.13f);
                Assert.That(completions, Is.EqualTo(1));
                Assert.That(fill.enabled, Is.True, "The solved channel stays filled.");
                flow.StopAndClear();
                Assert.That(fill.enabled, Is.False);
                Assert.That(fill.positionCount, Is.Zero);
                Assert.That(flow.Play(path, target, () => completions++), Is.True);
                flow.StopAndClear();
                flow.Advance(2);
                Assert.That(completions, Is.EqualTo(1), "Cancelled callbacks must never fire.");
                flow.Play(path, target, () => completions++);
                flow.Advance(.49f);
                flow.Advance(.15f);
                Assert.That(completions, Is.EqualTo(1),
                    "A slow frame crossing both timing thresholds must still show arrival before completion.");
                flow.Advance(.13f);
                Assert.That(completions, Is.EqualTo(2));
                Assert.That(root.transform.childCount, Is.EqualTo(1), "Flow visuals are reused across levels.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(targetObject); }
        }

        private static WorldGameplayTheme Theme(string world) => AssetDatabase.LoadAssetAtPath<WorldGameplayTheme>(
            "Assets/Data/GameplayThemes/" + world + "GameplayTheme.asset");

        [Test]
        public void TargetArrivalOccursOnceBeforeCompletionAndCancellationSuppressesIt()
        {
            GameObject root = new("ArrivalOrderTest");
            GameObject targetObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab"));
            try
            {
                TileView target = targetObject.GetComponent<TileView>();
                target.Initialize(new TileState(1, 0, TileShape.Straight, TileRole.Target, 0, true));
                EnergyFlowView flow = root.AddComponent<EnergyFlowView>();
                List<string> order = new();
                System.Action complete = () => order.Add("complete");
                System.Action arrive = () => order.Add("target");
                Vector3[] path = { Vector3.zero, Vector3.right };
                Assert.That(flow.Play(path, target, complete, "SakuraGarden", arrive), Is.True);
                flow.Advance(.49f);
                Assert.That(order, Is.Empty);
                flow.Advance(2f); // Overshooting must still preserve the visible arrival hold.
                Assert.That(order, Is.EqualTo(new[] { "target" }));
                flow.Advance(.11f);
                Assert.That(order, Is.EqualTo(new[] { "target" }));
                flow.Advance(.02f);
                flow.Advance(2f);
                Assert.That(order, Is.EqualTo(new[] { "target", "complete" }));
                flow.Play(path, target, complete, "SakuraGarden", arrive);
                flow.StopAndClear();
                flow.Advance(2f);
                Assert.That(order, Is.EqualTo(new[] { "target", "complete" }));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(targetObject); }
        }
    }
}
