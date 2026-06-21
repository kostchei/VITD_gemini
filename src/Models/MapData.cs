using System.Collections.Generic;
using VastDark.Common;

namespace VastDark.Models;

public class MapData
{
    public const int RegWidth = 10;
    public const int RegHeight = 8;
    public const int LocalRadius = 3; // Radius 3 to cover 6-mile flat-topped boundary (exactly 37 subhexes)

    public Dictionary<HexCoords, HexTile> RegionalTiles { get; } = new();
    
    // Maps a Regional Hex coordinate to its sub-grid of Local Hexes
    public Dictionary<HexCoords, Dictionary<HexCoords, HexTile>> LocalMaps { get; } = new();

    // Maps a Regional Hex coordinate to its list of mobile Local Hazards
    public Dictionary<HexCoords, List<Hazard>> LocalHazards { get; } = new();

    // Map dungeon entrances to their specific levels
    // Maps Local Hex coordinate (within a specific regional hex) -> Dungeon levels
    // We can use a composite key (RegionalCoords, LocalCoords) to uniquely identify a dungeon
    public Dictionary<(HexCoords Regional, HexCoords Local), List<DungeonLevel>> Dungeons { get; } = new();

    public void AddRegionalTile(HexTile tile)
    {
        RegionalTiles[tile.Coords] = tile;
    }

    public Dictionary<HexCoords, HexTile> GetOrCreateLocalMap(HexCoords regCoords, System.Func<HexCoords, Dictionary<HexCoords, HexTile>> generator)
    {
        if (!LocalMaps.TryGetValue(regCoords, out var localMap))
        {
            localMap = generator(regCoords);
            LocalMaps[regCoords] = localMap;
        }
        return localMap;
    }

    public List<Hazard> GetOrCreateLocalHazards(HexCoords regCoords, System.Func<HexCoords, List<Hazard>> generator)
    {
        if (!LocalHazards.TryGetValue(regCoords, out var hazards))
        {
            hazards = generator(regCoords);
            LocalHazards[regCoords] = hazards;
        }
        return hazards;
    }

    public List<DungeonLevel> GetOrCreateDungeon(HexCoords regCoords, HexCoords localCoords, System.Func<List<DungeonLevel>> generator)
    {
        var key = (regCoords, localCoords);
        if (!Dungeons.TryGetValue(key, out var levels))
        {
            levels = generator();
            Dungeons[key] = levels;
        }
        return levels;
    }
}
