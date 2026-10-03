namespace PipeMuzzle.Tests.EditMode
{
    public sealed class BambooLevelDefinitionTests : LevelDefinitionContentTests
    {
        protected override string WorldName => "BambooWorkshop";
        protected override string LevelPrefix => "Bamboo_";
        // Board width/height, route tiles, distractors, exact clockwise clicks, T-junctions, crosses.
        private static readonly (int width, int height, int path, int decoys, int moves, int tees, int crosses)[] Expected =
        {
            (5,4,12,4,14,2,0), (5,4,13,4,16,2,0), (5,4,14,4,17,3,0),
            (5,5,15,5,19,3,0), (5,5,16,5,20,3,0), (5,5,17,5,22,4,0),
            (6,5,18,6,24,4,0), (6,5,19,6,25,4,0), (6,5,20,6,27,4,0),
            (6,6,21,7,29,5,0), (6,6,22,8,31,5,0), (6,6,24,8,33,5,0)
        };
        protected override (int width, int height, int path, int decoys, int moves, int tees, int crosses)[] Budgets => Expected;
    }
}
