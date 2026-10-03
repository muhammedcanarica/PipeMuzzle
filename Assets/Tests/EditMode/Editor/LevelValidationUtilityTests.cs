using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class LevelValidationUtilityTests
    {
        [Test]
        public void SolverFindsCheapestClockwiseRotationWithShapeSymmetry()
        {
            BoardState board = new(5, 3);
            board.SetTile(new TileState(1, 1, TileShape.Corner, TileRole.Source, 0, true));
            board.SetTile(new TileState(2, 1, TileShape.Straight, TileRole.Normal, 0, false));
            board.SetTile(new TileState(3, 1, TileShape.Corner, TileRole.Target, 3, true));
            LevelValidationResult result = LevelValidationUtility.Analyze(board);
            Assert.That(result.MinimumMoves, Is.EqualTo(1));
            Assert.That(result.RouteCount, Is.EqualTo(1));
            Assert.That(result.ShortestPathLength, Is.EqualTo(3));
        }

        [Test]
        public void SolverRejectsWalkThatReusesCornerWithIncompatibleOrientations()
        {
            BoardState board = new(5, 5);
            board.SetTile(new TileState(2, 1, TileShape.Corner, TileRole.Source, 3, true));
            board.SetTile(new TileState(2, 3, TileShape.Corner, TileRole.Target, 2, true));
            board.SetTile(new TileState(2, 2, TileShape.Corner, TileRole.Normal, 0, false));
            (int x, int y, TileShape shape)[] loop =
            {
                (3,2,TileShape.Straight), (4,2,TileShape.Corner),
                (4,1,TileShape.Straight), (4,0,TileShape.Corner),
                (3,0,TileShape.Straight), (2,0,TileShape.Straight), (1,0,TileShape.Straight),
                (0,0,TileShape.Corner), (0,1,TileShape.Straight), (0,2,TileShape.Corner),
                (1,2,TileShape.Straight)
            };
            foreach (var tile in loop)
                board.SetTile(new TileState(tile.x, tile.y, tile.shape, TileRole.Normal, 0, false));
            Assert.That(LevelValidationUtility.Analyze(board).RouteCount, Is.Zero,
                "An edge-only search can traverse the same corner twice with incompatible ports.");
        }

        [Test]
        public void SolverComparesAlternateRoutesInsteadOfTrustingAnIntendedFixture()
        {
            BoardState board = new(5, 4);
            board.SetTile(new TileState(1,1,TileShape.Corner,TileRole.Source,0,true));
            board.SetTile(new TileState(3,1,TileShape.Corner,TileRole.Target,3,true));
            board.SetTile(new TileState(2,1,TileShape.Straight,TileRole.Normal,0,false));
            board.SetTile(new TileState(1,2,TileShape.Corner,TileRole.Normal,1,false));
            board.SetTile(new TileState(2,2,TileShape.Straight,TileRole.Normal,0,false));
            board.SetTile(new TileState(3,2,TileShape.Corner,TileRole.Normal,3,false));
            LevelValidationResult result = LevelValidationUtility.Analyze(board);
            Assert.That(result.RouteCount, Is.EqualTo(2));
            Assert.That(result.MinimumMoves, Is.EqualTo(1));
            Assert.That(result.SolutionPath.Count, Is.EqualTo(3));
        }

        [TestCase("duplicate", "Duplicate")]
        [TestCase("outside", "Out-of-bounds")]
        [TestCase("rotation", "Invalid tile data")]
        [TestCase("source", "one source")]
        [TestCase("port", "Initial port leaves board")]
        public void InvalidSerializedContentIsRejectedBeforeBuildingBoard(string mutation, string diagnostic)
        {
            LevelDefinition level = Object.Instantiate(AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                "Assets/Scripts/Data/Level_001.asset"));
            try
            {
                SerializedObject settings = new(level);
                SerializedProperty tiles = settings.FindProperty("tiles");
                SerializedProperty first = tiles.GetArrayElementAtIndex(0);
                switch (mutation)
                {
                    case "duplicate":
                        tiles.GetArrayElementAtIndex(1).FindPropertyRelative("x").intValue = first.FindPropertyRelative("x").intValue;
                        tiles.GetArrayElementAtIndex(1).FindPropertyRelative("y").intValue = first.FindPropertyRelative("y").intValue;
                        break;
                    case "outside": first.FindPropertyRelative("x").intValue = level.Width; break;
                    case "rotation": first.FindPropertyRelative("startRotation").intValue = 4; break;
                    case "source":
                        for (int i = 0; i < tiles.arraySize; i++)
                        {
                            SerializedProperty role = tiles.GetArrayElementAtIndex(i).FindPropertyRelative("role");
                            if (role.enumValueIndex == (int)TileRole.Source) role.enumValueIndex = (int)TileRole.Normal;
                        }
                        break;
                    case "port":
                        first.FindPropertyRelative("shape").enumValueIndex = (int)TileShape.Straight;
                        first.FindPropertyRelative("startRotation").intValue = 0;
                        break;
                }
                settings.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(LevelValidationUtility.Analyze(level).Errors.Any(e => e.Contains(diagnostic)), Is.True);
            }
            finally { Object.DestroyImmediate(level); }
        }
    }
}
