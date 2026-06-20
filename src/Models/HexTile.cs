using VastDark.Common;

namespace VastDark.Models;

public class HexTile
{
    public HexCoords Coords { get; }
    public string Biome { get; set; }
    public string Landmark { get; set; }
    public string Name { get; set; }

    public HexTile(HexCoords coords, string biome, string landmark = "None", string name = "")
    {
        Coords = coords;
        Biome = biome;
        Landmark = landmark;
        Name = string.IsNullOrEmpty(name) ? $"{biome} {coords}" : name;
    }
}
