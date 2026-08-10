using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public class BoardLogicTester : MonoBehaviour
    {
        [SerializeField]
        private BoardView boardView;

        [SerializeField]
        private LevelDefinition level;

        private BoardState board;
        private bool isCompleted;

        private void Start()
        {
            board = BoardBuilder.Build(level);

            boardView.TileClicked += HandleTileClicked;

            boardView.Build(board);

            bool solved = ConnectionChecker.Evaluate(board);

            Debug.Log($"Başlangıçta çözüldü mü: {solved}");
        }

        private void HandleTileClicked(TileView tileView)
        {
            if (isCompleted)
            {
                return;
            }

            TileState tile = tileView.State;

            bool rotated = board.TryRotateTile(
                tile.X,
                tile.Y
            );

            if (!rotated)
            {
                return;
            }

            tileView.Refresh();

            bool solved = ConnectionChecker.Evaluate(board);

            Debug.Log($"Hamle sayısı: {board.MoveCount}");
            Debug.Log($"Çözüldü mü: {solved}");

            if (solved)
            {
                isCompleted = true;

                Debug.Log("Bölüm tamamlandı!");
            }
        }

        private void OnDestroy()
        {
            if (boardView != null)
            {
                boardView.TileClicked -= HandleTileClicked;
            }
        }
    }
}