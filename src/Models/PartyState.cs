using System;
using System.Collections.Generic;
using VastDark.Common;

namespace VastDark.Models;

public class PartyState
{
    public List<Character> Members { get; set; } = new();
    public int Rations { get; set; } = 20;
    public int Gold { get; set; } = 100;
    public string DailyLog { get; set; } = "Start of day. The wastes are quiet.";
    public bool Sheltered { get; set; } = false;

    // Movement tracking
    public HexCoords LocalCoords { get; set; } = HexCoords.Zero;
    public int MilesTraveledToday { get; set; } = 0;
    public int TotalMilesTraveled { get; set; } = 0;
    public int MilesSinceLastEncounterCheck { get; set; } = 0;
    public int DailyMovementLimit { get; set; } = 18;
    public int ActiveWeatherEncounterMod { get; set; } = 0;
    public bool ForcedMarchActive { get; set; } = false;
    public int DungeonX { get; set; } = -1;
    public int DungeonY { get; set; } = -1;

    // Navigation Assets (Page 9)
    public bool HasLandmark { get; set; } = false;
    public bool HasDirections { get; set; } = false;
    public bool HasTool { get; set; } = false;
    public bool HasLight { get; set; } = false;
    public bool HasDeadReckoning { get; set; } = false;
    public bool UtterlyLostActive { get; set; } = false;
    public int UtterlyLostDays { get; set; } = 0;

    // Raw Lodestone Inventory (Page 14)
    public int RawLodestone { get; set; } = 0;

    // Pillar Delving State (Page 15)
    public bool InPillarDelve { get; set; } = false;
    public int DelveDepth { get; set; } = 0;
    public int DelvePreviousEventsCount { get; set; } = 0;
    public string DelveTunnelShape { get; set; } = "";

    public int GetNavigationAssetCount()
    {
        int count = 0;
        if (HasLandmark) count++;
        if (HasDirections) count++;
        if (HasTool) count++;
        if (HasLight) count++;
        if (HasDeadReckoning) count++;
        return count;
    }

    public PartyState()
    {
        // Default starting party members with distinct DCC stats (3d6 scale)
        // Name, STR, DEX, CON, WIS, INT, CHA
        Members.Add(new Character("Doran", 15, 11, 14, 9, 12, 8));    // CON +2, STR +2
        Members.Add(new Character("Lira", 9, 16, 12, 13, 14, 10));    // DEX +3, INT +2
        Members.Add(new Character("Vael", 8, 10, 11, 15, 12, 14));    // WIS +2, CHA +2
        Members.Add(new Character("Sylas", 12, 12, 13, 10, 9, 15));   // CON +1, CHA +2
    }
}
