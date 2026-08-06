using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public class BoardLogicTester : MonoBehaviour
    {
        private void Start()
        {
            BoardState board = new BoardState(3, 3);

            TileState source = new TileState(
                x: 0,
                y: 1,
                shape: TileShape.Straight,
                role: TileRole.Source,
                rotation: 1,
                isLocked: true);

            TileState middle = new TileState(
                x: 1,
                y: 1,
                shape: TileShape.Straight,
                role: TileRole.Normal,
                rotation: 0,
                isLocked: false);

            TileState target = new TileState(
                x: 2,
                y: 1,
                shape: TileShape.Straight,
                role: TileRole.Target,
                rotation: 1,
                isLocked: true);

            board.SetTile(source);
            board.SetTile(middle);
            board.SetTile(target);

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