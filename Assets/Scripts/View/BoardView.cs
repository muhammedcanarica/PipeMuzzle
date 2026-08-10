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
        [Min(0.01f)]
        private float tileSpacing = 1f;

        public event Action<TileView> TileClicked;

        public void Build(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            float centerX =
                (board.Width - 1) * tileSpacing * 0.5f;

            float centerY =
                (board.Height - 1) * tileSpacing * 0.5f;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    TileState tileState = board.GetTile(x, y);

                    if (tileState == null)
                    {
                        continue;
                    }

                    CreateTile(tileState, centerX, centerY);
                }
            }
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>();

            bounds = default;
            bool hasBounds = false;

            foreach (Renderer currentRenderer in renderers)
            {
                if (!currentRenderer.enabled ||
                    !currentRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = currentRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(currentRenderer.bounds);
            }

            return hasBounds;
        }

        private void CreateTile(
            TileState tileState,
            float centerX,
            float centerY)
        {
            TileView tileView = Instantiate(
                tilePrefab,
                transform
            );

            tileView.transform.localPosition = new Vector3(
                tileState.X * tileSpacing - centerX,
                tileState.Y * tileSpacing - centerY,
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
