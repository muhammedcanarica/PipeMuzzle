using System;
using UnityEngine;

namespace PipeMuzzle.Data
{
    [Serializable]
    public class TileDefinition
    {
        [SerializeField] private int x;
        [SerializeField] private int y;

        [SerializeField] private TileShape shape;
        [SerializeField] private TileRole role;

        [Range(0, 3)]
        [SerializeField] private int startRotation;

        [SerializeField] private bool isLocked;

        public int X => x;
        public int Y => y;

        public TileShape Shape => shape;
        public TileRole Role => role;

        public int StartRotation => startRotation;
        public bool IsLocked => isLocked;
    }
}