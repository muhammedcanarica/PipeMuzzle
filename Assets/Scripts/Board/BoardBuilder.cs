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
                    definition.IsLocked
                    );

                board.SetTile(tile);
            }
            return board;
        }
    }
}