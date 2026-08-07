using System;
using PipeMuzzle.Board;
using UnityEngine;

namespace PipeMuzzle.View
{
    public class TileView : MonoBehaviour
    {
        private TileState tileState;

        public TileState State => tileState;

        public void Initialize(TileState state)
        {
            if (state == null)
            {
                throw new ArgumentException(nameof(state));
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
        }
    }
}