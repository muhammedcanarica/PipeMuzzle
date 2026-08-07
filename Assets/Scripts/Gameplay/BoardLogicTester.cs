using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public class BoardLogicTester : MonoBehaviour
    {
        [SerializeField]
        private LevelDefinition level;

        private void Start()
        {
            BoardState board = BoardBuilder.Build(level);

            bool solvedBeforeRotation =
                ConnectionChecker.Evaluate(board);

            Debug.Log(
                $"Dönüşten önce çözüldü mü: {solvedBeforeRotation}");

            bool rotationSucceeded =
                board.TryRotateTile(1, 1);

            bool solvedAfterRotation =
                ConnectionChecker.Evaluate(board);

            Debug.Log(
                $"Parça döndü mü: {rotationSucceeded}");

            Debug.Log(
                $"Dönüşten sonra çözüldü mü: {solvedAfterRotation}");

            Debug.Log(
                $"Hamle sayısı: {board.MoveCount}");
        }
    }
}