using System;
using System.Collections.Generic;

namespace VastDark.Models;

public class Character
{
    public string Name { get; set; } = "";
    public int Level { get; set; } = 1;
    
    // Ability Scores (DCC convention: 3d6 scale, 3-18)
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Charisma { get; set; } = 10;

    // Grit & Flesh
    public int Grit { get; set; }
    public int MaxGrit { get; set; }
    public int Flesh { get; set; }
    public int MaxFlesh { get; set; }

    public int Exhaustion { get; set; } = 0; // 0 to 6. 6 is dead.
    public bool IsAlive { get; set; } = true;

    // Memories & Injuries
    public List<string> Memories { get; set; } = new();
    public Dictionary<string, bool> Injuries { get; set; } = new();

    // Map HP/MaxHP properties to delegate to Grit/Flesh system for backward compatibility
    public int HP 
    { 
        get => Grit; 
        set 
        {
            if (value < Grit)
            {
                ApplyDirectDamage(Grit - value);
            }
            else if (value > Grit)
            {
                HealDamage(value - Grit);
            }
        }
    }

    public int MaxHP 
    { 
        get => MaxGrit; 
        set => MaxGrit = value; 
    }

    public Character(string name, int str = 10, int dex = 10, int con = 10, int wis = 10, int @int = 10, int cha = 10)
    {
        Name = name;
        Strength = str;
        Dexterity = dex;
        Constitution = con;
        Wisdom = wis;
        Intelligence = @int;
        Charisma = cha;

        // Initialize Injuries
        Injuries["STR"] = false;
        Injuries["DEX"] = false;
        Injuries["CON"] = false;
        Injuries["WIS"] = false;
        Injuries["INT"] = false;
        Injuries["CHA"] = false;

        // Initialize Memories (Page 8: "5 memories or drives")
        Memories.Add("The smell of old books in my study");
        Memories.Add("My first kiss under the waning sky");
        Memories.Add("The taste of fresh fruit from home");
        Memories.Add("My mother's gentle humming");
        Memories.Add("A burning desire to see the sun");

        RecalculateMaxVitals();
        Grit = MaxGrit;
        Flesh = MaxFlesh;
    }

    public Character() 
    {
        Injuries["STR"] = false;
        Injuries["DEX"] = false;
        Injuries["CON"] = false;
        Injuries["WIS"] = false;
        Injuries["INT"] = false;
        Injuries["CHA"] = false;
    }

    public int GetModifier(int statValue)
    {
        return (int)Math.Floor((statValue - 10) / 2.0);
    }

    public int StrMod => GetModifier(Strength);
    public int DexMod => GetModifier(Dexterity);
    public int ConMod => GetModifier(Constitution);
    public int WisMod => GetModifier(Wisdom);
    public int IntMod => GetModifier(Intelligence);
    public int ChaMod => GetModifier(Charisma);

    public int GetHighestModifier()
    {
        return Math.Max(StrMod, 
               Math.Max(DexMod, 
               Math.Max(ConMod, 
               Math.Max(WisMod, 
               Math.Max(IntMod, ChaMod)))));
    }

    public void RecalculateMaxVitals()
    {
        // Max Grit = Level * 6 + CON bonus (min 1)
        MaxGrit = Math.Max(1, Level * 6 + ConMod); 
        
        // Flesh = Level + Highest ability bonus (min 1)
        MaxFlesh = Math.Max(1, Level + GetHighestModifier());
    }

    // Ability Check (DCC)
    public int RollCheck(string statName, Random random)
    {
        int modifier = statName switch
        {
            "STR" => StrMod,
            "DEX" => DexMod,
            "CON" => ConMod,
            "WIS" => WisMod,
            "INT" => IntMod,
            "CHA" => ChaMod,
            _ => 0
        };

        bool isInjured = Injuries.ContainsKey(statName) && Injuries[statName];
        if (isInjured)
        {
            // Disadvantage: roll twice, take lower
            int r1 = random.Next(1, 21);
            int r2 = random.Next(1, 21);
            return Math.Min(r1, r2) + modifier;
        }
        else
        {
            return random.Next(1, 21) + modifier;
        }
    }

    public void LoseMemory(System.Text.StringBuilder? log = null)
    {
        if (Memories.Count > 0)
        {
            string lost = Memories[Memories.Count - 1];
            Memories.RemoveAt(Memories.Count - 1);
            string msg = $"• {Name} has forgotten: '{lost}' ({Memories.Count}/5 memories remaining).";
            if (log != null) log.AppendLine(msg);
            else Console.WriteLine(msg);

            if (Memories.Count == 0)
            {
                IsAlive = false;
                string deathMsg = $"☠️ {Name} has lost all memories and wanders aimlessly into the dark...";
                if (log != null) log.AppendLine(deathMsg);
                else Console.WriteLine(deathMsg);
            }
        }
    }

    public void ApplyRandomInjury(System.Text.StringBuilder? log = null)
    {
        var stats = new List<string> { "STR", "DEX", "CON", "WIS", "INT", "CHA" };
        var random = new Random();
        string injuredStat = stats[random.Next(stats.Count)];
        Injuries[injuredStat] = true;
        string msg = $"  - ⚠️ {Name} suffered a {injuredStat} injury! Checks with this stat are now at disadvantage.";
        if (log != null) log.AppendLine(msg);
        else Console.WriteLine(msg);
    }

    public void ApplyDirectDamage(int damage)
    {
        if (!IsAlive) return;

        if (Grit >= damage)
        {
            Grit -= damage;
        }
        else
        {
            int overflow = damage - Grit;
            Grit = 0;
            
            int oldFlesh = Flesh;
            Flesh = Math.Max(0, Flesh - overflow);

            if (Flesh < oldFlesh && Flesh > 0)
            {
                ApplyRandomInjury();
                
                // Harrowing check on taking Flesh damage (1-in-6 chance)
                var random = new Random();
                if (random.Next(1, 7) == 1)
                {
                    LoseMemory();
                }
            }
            else if (Flesh == 0 && oldFlesh > 0)
            {
                LoseMemory();
                if (IsAlive)
                {
                    IsAlive = false;
                }
            }
        }
    }

    public void HealDamage(int amount)
    {
        // Restricted healing (Page 7): cannot heal if Flesh < MaxFlesh and not safe
        // Note: settlement doctors call HealFlesh directly, which is allowed.
        if (Flesh < MaxFlesh)
        {
            return; // Normal resting healing is blocked if Flesh is damaged
        }
        Grit = Math.Min(MaxGrit, Grit + amount);
    }

    public void HealFlesh(int amount)
    {
        Flesh = Math.Min(MaxFlesh, Flesh + amount);
        if (Flesh == MaxFlesh)
        {
            // Remove all injuries when Flesh is fully restored
            Injuries["STR"] = false;
            Injuries["DEX"] = false;
            Injuries["CON"] = false;
            Injuries["WIS"] = false;
            Injuries["INT"] = false;
            Injuries["CHA"] = false;
        }
    }
}
