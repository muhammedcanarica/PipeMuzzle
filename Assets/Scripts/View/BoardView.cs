using System;
using PipeMuzzle.Board;
using UnityEngine;

namespace PipeMuzzle.View
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField]
        private TileView tilePrefab;

        [SerializeField]
        private float tileSpacing = 1f;

        public event Action<TileView> TileClicked;

        public void Build(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    TileState tileState = board.GetTile(x, y);

                    if (tileState == null)
                    {
                        continue;
                    }

                    CreateTile(tileState);
                }
            }
        }

        private void CreateTile(TileState tileState)
        {
            TileView tileView = Instantiate(
                tilePrefab,
                transform
            );

            tileView.transform.localPosition = new Vector3(
                tileState.X * tileSpacing,
                tileState.Y * tileSpacing,
                0f
            );

            tileView.Initialize(tileState);

            tileView.Clicked += HandleTileClicked;
        }

        private void HandleTileClicked(TileView tileView)
        {
            TileClicked?.Invoke(tileView);
        }
    }
}