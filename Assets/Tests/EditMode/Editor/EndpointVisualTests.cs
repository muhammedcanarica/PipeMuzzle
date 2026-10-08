using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class EndpointVisualTests
    {
        private static readonly string[] Worlds = { "SakuraGarden", "BambooWorkshop", "MoonShrine" };

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void EndpointArtIsAnObjectRatherThanASmallMarker(string world)
        {
            var theme = Theme(world);
            foreach (Sprite sprite in new[] { theme.SourceMarker, theme.TargetMarker })
            {
                var pixels = new Texture2D(2, 2);
                try
                {
                    pixels.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                    int solid = pixels.GetPixels32().Count(c => c.a > 128);
                    Assert.That(solid, Is.GreaterThan(35000), "Endpoint needs a readable basin/tank silhouette.");
                    Assert.That(pixels.GetPixel(0, 0).a, Is.Zero);
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(410));
                    Assert.That(sprite.pivot, Is.EqualTo(Vector2.one * 313.5f));
                }
                finally { Object.DestroyImmediate(pixels); }
            }
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void TargetStaysEmptyWhenSolvedUntilArrivalThenRefreshResetsIt(string world)
        {
            GameObject root = Tile();
            try
            {
                TileView tile = root.GetComponent<TileView>();
                tile.ApplyTheme(Theme(world));
                var state = new TileState(0, 0, TileShape.Corner, TileRole.Target, 2, true);
                tile.Initialize(state);
                Transform water = root.transform.Find("EndpointWater");
                Assert.That(water, Is.Not.Null, "Target needs a separate arrival-only water layer.");
                var renderer = water.GetComponent<SpriteRenderer>();
                Assert.That(renderer.enabled, Is.False);
                state.SetPowered(true);
                tile.SetPowered(true, false);
                Assert.That(renderer.enabled, Is.False, "Connectivity preview is not flow arrival.");
                tile.PlayTargetImpact(1.06f, .22f);
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sprite, Is.Not.Null);
                Assert.That(Quaternion.Angle(water.rotation, Quaternion.identity), Is.LessThan(.01f));
                Assert.That(state.Rotation, Is.EqualTo(2));
                tile.Refresh();
                Assert.That(renderer.enabled, Is.False);
                Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void AuthoredEndpointsHaveExactlyOneLogicalPortTowardTheBakedPath(string worldName)
        {
            foreach (LevelDefinition level in Resources.Load<WorldDefinition>("Worlds/" + worldName).Levels)
            {
                BoardState board = BoardBuilder.Build(level);
                foreach (TileRole role in new[] { TileRole.Source, TileRole.Target })
                {
                    TileState tile = board.FindTileByRole(role);
                    int endpoint = role == TileRole.Source ? 0 : level.SolutionPath.Count - 1;
                    int next = role == TileRole.Source ? 1 : endpoint - 1;
                    Vector2Int delta = level.SolutionPath[next].Position - level.SolutionPath[endpoint].Position;
                    ConnectionMask expected = delta.x > 0 ? ConnectionMask.East : delta.x < 0 ? ConnectionMask.West
                        : delta.y > 0 ? ConnectionMask.North : ConnectionMask.South;
                    Assert.That(tile.Connections, Is.EqualTo(expected), level.name + " " + role);
                    Assert.That(tile.RotateClockwise(), Is.False);
                }
                foreach (LevelSolutionStep step in level.SolutionPath)
                {
                    TileState tile = board.GetTile(step.Position.x, step.Position.y);
                    tile.TryApplyHint(step.Rotation);
                }
                Assert.That(ConnectionChecker.Evaluate(board), Is.True, level.name);
            }
        }

        [TestCase(TileRole.Source)]
        [TestCase(TileRole.Target)]
        public void EndpointsRejectRotationEvenWhenAnAuthorOmitsTheLockFlag(TileRole role)
        {
            var tile = new TileState(0, 0, TileShape.Straight, role, 1, false);
            Assert.That(tile.RotateClockwise(), Is.False);
            Assert.That(tile.Rotation, Is.EqualTo(1));
        }

        private static IEnumerable<TestCaseData> Endpoints()
        {
            foreach (string world in Worlds)
            foreach (TileRole role in new[] { TileRole.Source, TileRole.Target })
            for (int rotation = 0; rotation < 4; rotation++)
                yield return new TestCaseData(world, role, rotation);
        }

        [TestCaseSource(nameof(Endpoints))]
        public void ConnectorKeepsOriginalTextureAndGridSeamsAtEveryRotation(string world, TileRole role, int rotation)
        {
            GameObject root = Tile();
            var pixels = new Texture2D(2, 2);
            try
            {
                TileView tile = root.GetComponent<TileView>();
                tile.ApplyTheme(Theme(world));
                var state = new TileState(0, 0, TileShape.Corner, role, rotation, true);
                tile.Initialize(state);
                Transform connector = root.transform.Find("EndpointConnector");
                Assert.That(connector, Is.Not.Null, "Endpoint connector overlay is missing.");
                Sprite original = root.GetComponent<SpriteRenderer>().sprite;
                var mesh = connector.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.bounds.extents.x, Is.EqualTo(original.bounds.extents.x).Within(.0001f));
                Assert.That(Quaternion.Angle(connector.rotation, root.transform.rotation), Is.LessThan(.01f));
                Assert.That(root.GetComponent<SpriteRenderer>().enabled, Is.False, "Normal pipe body must not obscure the object.");
                var properties = new MaterialPropertyBlock();
                connector.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
                Assert.That(properties.GetTexture("_MainTex"), Is.SameAs(original.texture));
                pixels.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(original)));
                Vector3[] sides = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
                ConnectionMask[] masks = { ConnectionMask.North, ConnectionMask.East, ConnectionMask.South, ConnectionMask.West };
                for (int i = 0; i < 4; i++)
                {
                    Vector3 local = Quaternion.Inverse(connector.rotation) * (sides[i] * .493f);
                    int x = Mathf.RoundToInt(original.pivot.x + local.x * original.pixelsPerUnit);
                    int y = Mathf.RoundToInt(original.pivot.y + local.y * original.pixelsPerUnit);
                    Assert.That(pixels.GetPixel(x, y).a, (state.Connections & masks[i]) != 0 ? Is.GreaterThan(.95f) : Is.LessThan(.01f));
                }
                var body = root.transform.Find("RoleVisual").GetComponent<SpriteRenderer>();
                Assert.That(Quaternion.Angle(body.transform.rotation, Quaternion.identity), Is.LessThan(.01f));
                Assert.That(connector.GetComponent<MeshRenderer>().sortingOrder, Is.LessThan(2), "Existing flow must render over the inlet/outlet.");
            }
            finally { Object.DestroyImmediate(pixels); Object.DestroyImmediate(root); }
        }

        [Test]
        public void AllThirtySixLevelsKeepBothEndpointsAndTheirOriginalBoardPositions()
        {
            int count = 0;
            foreach (string world in Worlds)
            foreach (LevelDefinition level in Resources.Load<WorldDefinition>("Worlds/" + world).Levels)
            {
                BoardState board = BoardBuilder.Build(level);
                foreach (TileRole role in new[] { TileRole.Source, TileRole.Target })
                {
                    var authored = level.Tiles.Single(t => t.Role == role);
                    TileState tile = board.FindTileByRole(role);
                    Assert.That(new Vector2Int(tile.X, tile.Y), Is.EqualTo(new Vector2Int(authored.X, authored.Y)));
                    Assert.That(tile.Rotation, Is.EqualTo(authored.StartRotation));
                }
                count++;
            }
            Assert.That(count, Is.EqualTo(36));
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void EveryAuthoredEndpointShowsOnlyThePortTowardItsSolutionNeighbor(string worldName)
        {
            var root = new GameObject("EndpointSinglePortBoard");
            try
            {
                BoardView view = root.AddComponent<BoardView>();
                typeof(BoardView).GetField("tilePrefab", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(view, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab").GetComponent<TileView>());
                WorldDefinition world = Resources.Load<WorldDefinition>("Worlds/" + worldName);
                view.SetGameplayTheme(world.GameplayTheme);
                MethodInfo build = typeof(BoardView).GetMethod("Build", new[] { typeof(BoardState), typeof(IReadOnlyList<LevelSolutionStep>) });
                Assert.That(build, Is.Not.Null, "Board presentation must use the authored endpoint neighbor.");
                foreach (LevelDefinition level in world.Levels)
                {
                    BoardState board = BoardBuilder.Build(level);
                    build.Invoke(view, new object[] { board, level.SolutionPath });
                    foreach (TileView tile in root.GetComponentsInChildren<TileView>().Where(t => t.State.Role != TileRole.Normal))
                    {
                        int last = level.SolutionPath.Count - 1;
                        Vector2Int neighbor = tile.State.Role == TileRole.Source ? level.SolutionPath[1].Position : level.SolutionPath[last - 1].Position;
                        Vector3 port = new Vector3(neighbor.x - tile.State.X, neighbor.y - tile.State.Y, 0);
                        Transform connector = tile.transform.Find("EndpointConnector");
                        Mesh mesh = connector.GetComponent<MeshFilter>().sharedMesh;
                        Assert.That(mesh.vertexCount, Is.EqualTo(4), "Only one cropped pipe arm should be visible.");
                        foreach (Vector3 vertex in mesh.vertices)
                            Assert.That(Vector3.Dot(connector.TransformDirection(vertex), port), Is.GreaterThan(.21f), level.name);
                        Assert.That(tile.State.Shape, Is.EqualTo(TileShape.Corner));
                        Assert.That(tile.State.Rotation, Is.EqualTo(level.Tiles.Single(t => t.Role == tile.State.Role).StartRotation));
                    }
                    Assert.That(board.MoveCount, Is.Zero);
                    Assert.That(ConnectionChecker.Evaluate(board), Is.False);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject Tile() => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab"));
        private static WorldGameplayTheme Theme(string world) => Resources.Load<WorldDefinition>("Worlds/" + world).GameplayTheme;
    }
}
