using PipeMuzzle.Board;
using PipeMuzzle.Data;

namespace PipeMuzzle.Gameplay
{
    public static class HintSelector
    {
        public static TileState Select(BoardState board, LevelDefinition level)
        {
            LevelSolutionStep step = SelectStep(board, level);
            return step == null ? null : board.GetTile(step.Position.x, step.Position.y);
        }

        public static LevelSolutionStep SelectStep(BoardState board, LevelDefinition level)
        {
            if (board == null || level == null || board.Width != level.Width || board.Height != level.Height)
                return null;

            foreach (LevelSolutionStep step in level.SolutionPath)
            {
                if (step == null || step.Rotation < 0 || step.Rotation > 3) return null;
                TileState tile = board.GetTile(step.Position.x, step.Position.y);
                if (tile == null) return null;
                if (tile.Role != TileRole.Normal || tile.IsLocked || tile.IsHintLocked || tile.Shape == TileShape.Empty) continue;
                ConnectionMask solved = tile.Shape.GetBaseConnections();
                for (int i = 0; i < step.Rotation; i++) solved = solved.RotateClockwise();
                if (tile.Connections != solved) return step;
            }
            return null;
        }
    }
}
