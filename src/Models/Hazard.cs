using System;
using VastDark.Common;

namespace VastDark.Models;

public class Hazard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Type { get; set; } // 1 to 6
    public HexCoords Coords { get; set; }
    public bool IsActive { get; set; } = true;

    public string Name => Type switch
    {
        1 => "Warband",
        2 => "Maelstrom",
        3 => "Crawlherd",
        4 => "Collapse",
        5 => "Void Lightning",
        6 => "Singing Sand",
        _ => "Unknown Hazard"
    };

    public string Description => Type switch
    {
        1 => "5d6 Cutthroats led by a Demagogue. Attacks on sight, but will not pursue players into Ruins or Settlements.",
        2 => "Violent swirling air columns. Caught characters are flung 1 mile in a random direction and suffer 3d20 damage.",
        3 => "A writhing horde of 1d20 Crawl creatures that haunt the wastes and ruins.",
        4 => "Crumbling ceiling debris falls. Travelers must gain exhaustion to run or be crushed. Ruins/Settlements have a 2-in-6 chance to become Wastes.",
        5 => "Jet-black lightning bolts. Metal-wielding characters have a 3-in-6 chance of being struck for 10d6 damage.",
        6 => "Quicksand vibrations from deep below. Save v. Breath or disappear into the ground. Avoided on high/solid ground.",
        _ => "A mysterious anomaly in the wastes."
    };

    public Hazard(int type, HexCoords coords)
    {
        Type = type;
        Coords = coords;
    }

    public Hazard() { }
}
