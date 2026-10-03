namespace PipeMuzzle.Tests.EditMode
{
    public sealed class SakuraLevelDefinitionTests : LevelDefinitionContentTests
    {
        protected override string WorldName => "SakuraGarden";
        protected override string LevelPrefix => "";
        // Board width/height, route tiles, distractors, exact clockwise clicks, T-junctions, crosses.
        private static readonly (int width, int height, int path, int decoys, int moves, int tees, int crosses)[] Expected =
        {
            (3,3,3,0,1,0,0), (4,3,5,1,3,0,0), (4,3,7,1,4,0,0),
            (4,4,8,2,6,0,0), (4,4,9,3,7,0,0), (4,4,10,3,8,1,0),
            (5,4,11,4,10,1,0), (5,4,12,4,11,2,0), (5,4,13,4,12,2,0),
            (5,5,14,5,14,2,0), (5,5,15,5,15,3,0), (5,5,16,6,17,3,0)
        };
        protected override (int width, int height, int path, int decoys, int moves, int tees, int crosses)[] Budgets => Expected;
    }
}
