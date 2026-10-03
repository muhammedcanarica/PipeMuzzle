using System;
using System.Collections.Generic;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("tileSpacing")]
        private float cellSize = 1f;

        private readonly Dictionary<Vector2Int, TileView> tileViews = new();
        private readonly List<Vector3> flowPathPositions = new();
        private WorldGameplayTheme gameplayTheme;
        private int boardWidth;
        private int boardHeight;
        private PipeHintFeedback hintFeedback;

        public bool IsHintPlaying => hintFeedback != null && hintFeedback.IsPlaying;

        public bool TryShowHint(TileState tile, Color color)
        {
            if (tile == null || !tileViews.TryGetValue(new Vector2Int(tile.X, tile.Y), out TileView view)) return false;
            if (hintFeedback == null) hintFeedback = gameObject.AddComponent<PipeHintFeedback>();
            return hintFeedback.TryShow(view, cellSize * transform.lossyScale.x, color);
        }

        public void StopHint() => hintFeedback?.StopAndClear();

        public event Action<TileView> TileClicked;

        public bool HasPendingRotations
        {
            get
            {
                foreach (TileView tile in tileViews.Values)
                    if (tile.HasPendingRotation) return true;
                return false;
            }
        }

        public void SetGameplayTheme(WorldGameplayTheme theme)
        {
            gameplayTheme = theme;
            if (energyFlowView != null) energyFlowView.Configure(theme);
        }

        private void Awake()
        {
            EnsureFlowView();
        }

        private void EnsureFlowView()
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

            EnsureFlowView();
            Clear();

            boardWidth = board.Width;
            boardHeight = board.Height;

            float centerX =
                (board.Width - 1) * cellSize * 0.5f;

            float centerY =
                (board.Height - 1) * cellSize * 0.5f;

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

                // Flow renderers belong to EnergyFlowView and are reused across levels.
                if (tileView == null) continue;

                tileView.Clicked -= HandleTileClicked;

                child.SetActive(false);
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            tileViews.Clear();
            boardWidth = 0;
            boardHeight = 0;
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

        public bool PlayCompletionFeedback(
            IReadOnlyList<TileState> solvedPath, Action onCompleted = null, string worldId = null)
        {
            return PlayEnergyFlow(solvedPath, onCompleted, worldId);
        }

        public void StopTransientEffects()
        {
            StopHint();
            if (energyFlowView != null)
            {
                energyFlowView.StopAndClear();
            }

            flowPathPositions.Clear();
            foreach (TileView tile in tileViews.Values) tile.Refresh();
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

        public bool TryGetGridBounds(out Bounds bounds)
        {
            bounds = default;
            if (boardWidth <= 0 || boardHeight <= 0) return false;

            bounds = new Bounds(
                transform.position,
                new Vector3(
                    boardWidth * cellSize,
                    boardHeight * cellSize,
                    0f
                )
            );
            return true;
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

            tileView.transform.localPosition = GetCellCenterLocalPosition(
                tileState.X,
                tileState.Y,
                centerX,
                centerY
            );
            tileView.ApplyTheme(gameplayTheme);

            tileView.Initialize(tileState);

            tileView.Clicked += HandleTileClicked;

            tileViews[new Vector2Int(tileState.X, tileState.Y)] = tileView;
        }

        private Vector3 GetCellCenterLocalPosition(
            int column,
            int row,
            float centerX,
            float centerY)
        {
            return new Vector3(
                column * cellSize - centerX,
                row * cellSize - centerY,
                0f
            );
        }

        private bool PlayEnergyFlow(
            IReadOnlyList<TileState> solvedPath, Action onCompleted, string worldId)
        {
            if (energyFlowView == null ||
                solvedPath == null ||
                solvedPath.Count < 2)
            {
                return false;
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
                    return false;
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
                energyFlowView.Configure(gameplayTheme);
                return energyFlowView.Play(flowPathPositions, targetTile, onCompleted, worldId);
            }
            return false;
        }

        private void HandleTileClicked(TileView tileView)
        {
            TileClicked?.Invoke(tileView);
        }
    }
}
