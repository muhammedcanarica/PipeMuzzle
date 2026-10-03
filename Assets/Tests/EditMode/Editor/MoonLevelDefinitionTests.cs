namespace PipeMuzzle.Tests.EditMode
{
    public sealed class MoonLevelDefinitionTests : LevelDefinitionContentTests
    {
        protected override string WorldName => "MoonShrine";
        protected override string LevelPrefix => "Moon_";
        // Board width/height, route tiles, distractors, exact clockwise clicks, T-junctions, crosses.
        private static readonly (int width, int height, int path, int decoys, int moves, int tees, int crosses)[] Expected =
        {
            (6,5,21,6,29,4,1), (6,6,22,7,31,4,1), (6,6,23,7,33,4,1),
            (6,6,24,7,35,4,2), (6,6,25,7,36,4,2), (6,6,26,7,38,4,2),
            (7,6,27,9,39,5,2), (7,6,28,8,41,5,2), (7,6,29,8,43,5,2),
            (7,7,30,11,45,5,3), (7,7,31,11,47,5,3), (7,7,32,10,50,6,3)
        };
        protected override (int width, int height, int path, int decoys, int moves, int tees, int crosses)[] Budgets => Expected;
    }
}
