using System;
using PipeMuzzle.Data;

namespace PipeMuzzle.Board
{
    public static class BoardBuilder
    {
        public static BoardState Build(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            BoardState board =
                new BoardState(level.Width, level.Height);

            foreach (TileDefinition definition in level.Tiles)
            {
                if (definition == null)
                {
                    continue;
                }

                TileState tile = new TileState(
                    definition.X,
                    definition.Y,
                    definition.Shape,
                    definition.Role,
                    definition.StartRotation,
                    definition.IsLocked,
                    EndpointConnection(level, definition)
                    );

                board.SetTile(tile);
            }
            return board;
        }

        private static ConnectionMask? EndpointConnection(LevelDefinition level, TileDefinition tile)
        {
            if (tile.Role == TileRole.Normal || level.SolutionPath.Count < 2) return null;
            int endpoint = tile.Role == TileRole.Source ? 0 : level.SolutionPath.Count - 1;
            int neighbor = tile.Role == TileRole.Source ? 1 : endpoint - 1;
            LevelSolutionStep end = level.SolutionPath[endpoint];
            LevelSolutionStep next = level.SolutionPath[neighbor];
            if (end == null || next == null || end.Position != new UnityEngine.Vector2Int(tile.X, tile.Y)) return null;
            UnityEngine.Vector2Int delta = next.Position - end.Position;
            if (UnityEngine.Mathf.Abs(delta.x) + UnityEngine.Mathf.Abs(delta.y) != 1) return null;
            return delta.x > 0 ? ConnectionMask.East : delta.x < 0 ? ConnectionMask.West
                : delta.y > 0 ? ConnectionMask.North : ConnectionMask.South;
        }
    }
}