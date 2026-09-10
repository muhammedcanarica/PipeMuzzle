using System;
using System.Collections.Generic;
using PipeMuzzle.Board;
using UnityEngine;

namespace PipeMuzzle.View
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField]
        private TileView tilePrefab;

        [SerializeField]
        private EnergyFlowView energyFlowView;

        [SerializeField]
        [Min(0.01f)]
        private float tileSpacing = 1f;

        private readonly Dictionary<Vector2Int, TileView> tileViews = new();
        private readonly List<Vector3> flowPathPositions = new();

        public event Action<TileView> TileClicked;

        private void Awake()
        {
            if (energyFlowView == null)
            {
                energyFlowView = GetComponent<EnergyFlowView>();
            }

            if (energyFlowView == null)
            {
                energyFlowView = gameObject.AddComponent<EnergyFlowView>();
            }
        }

        public void Build(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Clear();

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

                    CreateTile(
                        tileState,
                        centerX,
                        centerY
                    );
                }
            }
        }

        public void Clear()
        {
            StopTransientEffects();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;

                TileView tileView =
                    child.GetComponent<TileView>();

                if (tileView != null)
                {
                    tileView.Clicked -= HandleTileClicked;
                }

                child.SetActive(false);
                Destroy(child);
            }

            tileViews.Clear();
        }

        public void RefreshPoweredTiles(bool animated)
        {
            foreach (TileView tileView in tileViews.Values)
            {
                tileView.SetPowered(
                    tileView.State.IsPowered,
                    animated
                );
            }
        }

        public void PlayCompletionFeedback(
            IReadOnlyList<TileState> solvedPath)
        {
            foreach (TileView tileView in tileViews.Values)
            {
                tileView.PlayCompletionPulse();
            }

            PlayEnergyFlow(solvedPath);
        }

        public void StopTransientEffects()
        {
            if (energyFlowView != null)
            {
                energyFlowView.StopAndClear();
            }

            flowPathPositions.Clear();
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

            tileViews[new Vector2Int(tileState.X, tileState.Y)] = tileView;
        }

        private void PlayEnergyFlow(
            IReadOnlyList<TileState> solvedPath)
        {
            if (energyFlowView == null ||
                solvedPath == null ||
                solvedPath.Count < 2)
            {
                return;
            }

            flowPathPositions.Clear();

            for (int i = 0; i < solvedPath.Count; i++)
            {
                TileState state = solvedPath[i];
                Vector2Int coordinate = new Vector2Int(state.X, state.Y);

                if (!tileViews.TryGetValue(
                        coordinate,
                        out TileView tileView))
                {
                    flowPathPositions.Clear();
                    return;
                }

                flowPathPositions.Add(tileView.transform.position);
            }

            TileState targetState = solvedPath[solvedPath.Count - 1];
            Vector2Int targetCoordinate =
                new Vector2Int(targetState.X, targetState.Y);

            if (tileViews.TryGetValue(
                    targetCoordinate,
                    out TileView targetTile))
            {
                energyFlowView.Play(flowPathPositions, targetTile);
            }
        }

        private void HandleTileClicked(TileView tileView)
        {
            TileClicked?.Invoke(tileView);
        }
    }
}
