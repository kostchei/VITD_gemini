using System;

namespace VastDark.Models;

public class Character
{
    public string Name { get; set; } = "";
    public int HP { get; set; } = 30;
    public int MaxHP { get; set; } = 30;
    public int Exhaustion { get; set; } = 0; // 0 to 6. 6 is dead.
    public bool IsAlive { get; set; } = true;

    public Character(string name, int maxHp = 30)
    {
        Name = name;
        MaxHP = maxHp;
        HP = maxHp;
    }

    public Character() { }
}
