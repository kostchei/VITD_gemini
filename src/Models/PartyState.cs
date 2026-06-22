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

    public PartyState()
    {
        // Default starting party members
        Members.Add(new Character("Doran", 35));
        Members.Add(new Character("Lira", 25));
        Members.Add(new Character("Vael", 20));
        Members.Add(new Character("Sylas", 30));
    }
}
