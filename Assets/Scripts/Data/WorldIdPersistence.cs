using System;

namespace PipeMuzzle.Data
{
    public static class WorldIdPersistence
    {
        public static string Segment(WorldId id) => id switch
        {
            WorldId.SakuraGarden => "SakuraGarden",
            WorldId.BambooWorkshop => "BambooWorkshop",
            WorldId.MoonShrine => "MoonShrine",
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }
}
