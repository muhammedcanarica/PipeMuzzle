using System;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.View
{
    public class TileView : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer spriteRenderer;

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

        private void RefreshColor()
        {
            switch (tileState.Role)
            {
                case TileRole.Source:
                    spriteRenderer.color = Color.green;
                    break;

                case TileRole.Target:
                    spriteRenderer.color = Color.red;
                    break;

                default:
                    spriteRenderer.color = Color.white;
                    break;
            }
        }
    }
}