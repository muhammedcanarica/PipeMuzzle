using System;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.View
{
    public class TileView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [Header("Tile Sprites")]
        [SerializeField]
        private Sprite straightSprite;

        [SerializeField]
        private Sprite cornerSprite;

        [SerializeField]
        private Sprite threeWaySprite;

        [SerializeField]
        private Sprite crossSprite;

        private TileState tileState;

        public TileState State => tileState;

        public event Action<TileView> Clicked;

        public void Initialize(TileState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            tileState = state;

            Refresh();
        }

        public void Refresh()
        {
            if (tileState == null)
            {
                return;
            }

            RefreshSprite();

            transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                tileState.Rotation * -90f
            );

            RefreshColor();
        }

        private void OnMouseDown()
        {
            Clicked?.Invoke(this);
        }

        private void RefreshSprite()
        {
            switch (tileState.Shape)
            {
                case TileShape.Straight:
                    spriteRenderer.sprite = straightSprite;
                    break;

                case TileShape.Corner:
                    spriteRenderer.sprite = cornerSprite;
                    break;

                case TileShape.ThreeWay:
                    spriteRenderer.sprite = threeWaySprite;
                    break;

                case TileShape.Cross:
                    spriteRenderer.sprite = crossSprite;
                    break;

                case TileShape.Empty:
                    spriteRenderer.sprite = null;
                    break;
            }
        }

        private void RefreshColor()
        {
            switch (tileState.Role)
            {
                case TileRole.Source:
                    spriteRenderer.color = Color.white;
                    break;

                case TileRole.Target:
                    spriteRenderer.color = Color.white;
                    break;

                default:
                    spriteRenderer.color = Color.white;
                    break;
            }
        }
    }
}