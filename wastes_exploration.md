# Wastes Exploration: Weather, Encounters, and Curiosities

This document provides a complete summary of the Wastes exploration tables, translates their mechanical effects into clear rules, and outlines structured pseudo-code to simulate daily exploration, movement, and survival in the wastes.

---

## 1. Overview & Mechanics

When characters spend a day exploring or traveling through the Wastes, the Game Master (or game engine) rolls for **Weather** and **Encounters**. If an encounter occurs, their **Mood** is determined. Additionally, characters can discover **Curiosities** along their journey.

### Core Rules:
1. **Daily Rolls**: Roll **2d6 for Weather** and a combined **1d12 + 1d6 for Encounters** (giving a range of 2–18) every day spent in the wastes.
2. **Synergy / Modifiers**:
   - Certain weather conditions (like *Pillar Fog*) add modifiers (e.g., $+6$) to the daily encounter roll, pushing the results into dangerous high-tier encounters (rolls 13–18).
   - The rolled **1d6** is also reused directly as the **Mood** roll for the encounter, removing the need for a separate die roll.
3. **Landmarks & Travel**:
   - Thick storms or fog render landmarks invisible, which increases the chance of getting lost or prevents navigation.
   - Severe weather directly reduces daily travel progress (in miles).

---

## 2. Movement & Survival Rules

* **Base Travel Speed**: Travelers on foot average **18 miles per day (24 hours)**.
* **Forced March**: Travelers can push themselves to travel an extra **6 miles** (total of 24 miles base) at the cost of gaining **1 level of exhaustion** for each character.
* **Starvation**: Each traveler must consume **1 ration per day**. Any traveler who does not eat gains **1 level of exhaustion**.
* **Terrain Impact**: Daily weather and encounter rolls are determined by the current hex's terrain type (e.g., Wastes, Ruins, or Pillars).

---

## 3. Weather Table (2D6)

*Note: This table applies when the party is in the Wastes terrain.*

| Roll (2d6) | Weather Condition | Description & Gameplay Effects |
| :---: | :--- | :--- |
| **1–6** | **Calm** | Chill gusts of wind and the settling of dust. No mechanical penalties. |
| **7** | **Dust Storm** | Dust is whipped into a blinding frenzy.<br>• Travel progress is reduced by **6 miles**.<br>• Impossible to make out landmarks. |
| **8** | **Wind Blast** | Vicious wind strikes at the unsuspecting.<br>• Unprotected lights and fires are blown out.<br>• Unprotected travelers caught in the open must Save or take **3d6 damage**. |
| **9** | **Stone Hail** | The cavern ceiling rumbles and stones rain down.<br>• Unprotected travelers must **Save vs. Breath** or suffer **3d6 damage**. |
| **10** | **Pillar Fog** | The caverns belch a chill and ominous mist.<br>• Adds **+6** to the encounter roll for the day.<br>• Impossible to make out landmarks. |
| **11** | **Grit Slide** | Dunes shift and collapse like avalanches.<br>• Must **Save vs. Breath** or suffer **3d6 damage** and lose **6 miles** of progress. |
| **12** | **Dune Wave** | A tectonic shift sends waves of dust/sand across the wastes.<br>• Every traveler must gain a level of **Exhaustion** running from the waves or be **Buried**. |

---

## 4. Encounters & Moods Table (1D12 + 1D6 + Modifiers)

*Note: This table applies when the party is in the Wastes terrain.*

Roll **1d12 and 1d6** together and sum them (+ weather modifiers if applicable). The resulting total determines the encounter type (ranging from 2–18). If the encounter lists a **Mood**, the face of the rolled **1d6** is reused directly to determine their behavior.

| Roll | Encounter | Quantity | Mood / Behavior Sub-Table (1d6) |
| :---: | :--- | :---: | :--- |
| **1–5** | **Nothing** | — | You are alone for now... |
| **6** | **Lost Travelers** | $1d6$ | Desperate for food and shelter. Helpful if assisted. |
| **7** | **Nomads** | $1d6$ | Braving the weather of the wastes.<br>• **1**: Cautious, hostile if disturbed.<br>• **2–4**: Curious, peaceful if hailed.<br>• **5–6**: Friendly, gives directions and warns of nearby dangers. |
| **8** | **Merchants** | $1d3$ | Carrying wares on a heavy pulk. Willing to trade/buy/sell (Limit: 100 coins). |
| **9** | **Bandits** | $1d6$ | Prowling for an easy score.<br>• **1–2**: Crazed, attacks to kill and loot.<br>• **3–5**: Tribute, demands 100 coins or a ration from each traveler.<br>• **6**: Curious, lets characters join in a raid. |
| **10** | **Pilgrims** | $2d6$ | Devoted to a random faction. Friendly if characters join, hostile if contested. |
| **11** | **Lodestone Prospectors** | $1d6$ | Carrying $1d20$ raw lodestones on a sleigh. Cautious of strangers, hostile if harassed. |
| **12** | **Caravan** | $1d6$ Mech, $2d6$ Nom | Carrying wares on a heavy pulk. Willing to trade/buy/sell (Limit: 1000 coins). |
| **13** | **Cutthroats** | $1d6$ | Out for blood and plunder.<br>• **1–3**: Crazed, attacks to kill and loot.<br>• **4–5**: Tribute, demands 1000 coins or all rations.<br>• **6**: Recruit, demands party members duel; survivor joins.<br> |
| **14** | **Cyclops** | $1d6$ | Clustered tightly together for warmth, smelling the air for unwary mortals. |
| **15** | **Harpies** | $1d3$ | Circling above or buried under the dust, patiently waiting. |
| **16** | **Medusa** | $1d3$ | Curled in corners and hidden spots, listening for the sound of steps. |
| **17** | **Shade** | 1 | Drifting through the air, vibrating with hunger. |
| **18** | **Griffon** | 1 | Resting on the highest point, with the remains of its latest victim nearby. |

---

## 5. Curiosities Table (1D20)

Roll **1d20** when searching, exploring landmarks, or finding points of interest:

| Roll (1d20) | Curiosity Discovery | Description & Mechanical Rules / Loot |
| :---: | :--- | :--- |
| **1** | Ruin outcropping | Provides shelter. Has a 1-in-6 chance of triggering a random encounter. |
| **2** | Abandoned camp | Shows signs of violence. Contains **1d3 rations** and a random tool. |
| **3** | Stone totem | Geometric designs with offerings of effigies at its base. |
| **4** | Desiccated corpses | Dead nomads; one utters a single word before expiring... |
| **5** | Burial cairn | Partially buried. Can loot for a random weapon and a random tool. |
| **6** | Cache of lodestone | Contains **1d10 x 5 lodestones** hidden under a stone with an illegible note. |
| **7** | Nomad in black | Silent and still. Points hand toward the nearest pillar. |
| **8** | Collapsed tower | The remains of a vast ruin. Provides shelter. |
| **9** | Lodestone obelisk | Etched with symbols. Can mine for **1d20 Raw Lodestones**. |
| **10** | Tied traveler | Tied to a stone pillar. Helpful if freed, but has no memory of the past. |
| **11** | Unearthed road | Leads to the nearest ruins. Counts as a landmark. |
| **12** | Insect swarm | Stinging, whirring insects buffeted by wind. Harvest for **1d3 rations**. |
| **13** | Lonely graves | Sand-covered graves with bodies placed carefully. |
| **14** | Worm nest | Eyeless worms or small crabs in soft dust. Harvest for **1d6 rations**. |
| **15** | Crawl corpse | Rotten and massive. Many mouths feed on its remains. |
| **16** | Secret Tunnel | Provides shelter. Travel through it moves party **1d6 miles** in a random direction. |
| **17** | Message slab | Carved message on stone slab, mostly illegible from wear and wind. |
| **18** | Lost Caravan | Corpses and goods buried by dust. Salvage for **1d20 rations or tools**. |
| **19** | Bereft Swordsman | Blade is nearly rusted away. Only repeats: *"There is no way out."* |
| **20** | Forgotten treasure | Buried and lost. Roll on the Treasure Table (pg. 29). |

---

## 6. Algorithmic Pseudo-code

Below is a complete structured representation of the weather, encounter, and curiosity rolls mapped into an exploration simulation framework incorporating movement and survival rules.

### Data Structures & Enums

```python
enum TerrainType:
    WASTES
    RUINS
    PILLARS

enum WeatherType:
    CALM
    DUST_STORM
    WIND_BLAST
    STONE_HAIL
    PILLAR_FOG
    GRIT_SLIDE
    DUNE_WAVE

class WeatherEffect:
    type: WeatherType
    travel_penalty_miles: int
    landmarks_visible: bool
    encounter_modifier: int

class Character:
    name: String
    hp: int
    max_hp: int
    exhaustion: int      # Ranges 0 to 6 (6 is death)
    is_alive: bool

class PartyState:
    members: List[Character]
    rations: int
    gold: int
    sheltered: bool
    current_terrain: TerrainType
```

### Weather Resolver (Terrain-Based)

```python
function roll_daily_weather(terrain: TerrainType, party: PartyState) -> WeatherEffect:
    effect = WeatherEffect()
    effect.travel_penalty_miles = 0
    effect.landmarks_visible = True
    effect.encounter_modifier = 0
    
    # 1. Resolve Weather based on Terrain
    if terrain == TerrainType.PILLARS:
        # Pillars has custom wind/fog rules; shown here as standard placeholder
        effect.type = WeatherType.CALM
        return effect
        
    elif terrain == TerrainType.RUINS:
        # Ruins provides wind protection, less severe storms
        effect.type = WeatherType.CALM
        return effect
        
    # Default: TerrainType.WASTES
    roll = roll_dice(count=2, sides=6) # 2d6
    
    if roll <= 6:
        effect.type = WeatherType.CALM
        
    elif roll == 7:
        effect.type = WeatherType.DUST_STORM
        effect.travel_penalty_miles = 6
        effect.landmarks_visible = False
        
    elif roll == 8:
        effect.type = WeatherType.WIND_BLAST
        extinguish_party_lights(party)
        if not party.sheltered:
            for char in party.members:
                if char.is_alive and roll_save(char, "breath") == Failed:
                    apply_damage(char, roll_dice(3, 6))
                    
    elif roll == 9:
        effect.type = WeatherType.STONE_HAIL
        if not party.sheltered:
            for char in party.members:
                if char.is_alive and roll_save(char, "breath") == Failed:
                    apply_damage(char, roll_dice(3, 6))
                    
    elif roll == 10:
        effect.type = WeatherType.PILLAR_FOG
        effect.encounter_modifier = 6
        effect.landmarks_visible = False
        
    elif roll == 11:
        effect.type = WeatherType.GRIT_SLIDE
        effect.travel_penalty_miles = 6
        if not party.sheltered:
            for char in party.members:
                if char.is_alive and roll_save(char, "breath") == Failed:
                    apply_damage(char, roll_dice(3, 6))
                    
    elif roll == 12:
        effect.type = WeatherType.DUNE_WAVE
        for char in party.members:
            if char.is_alive:
                if roll_save(char, "reflex/breath") == Success:
                    apply_exhaustion(char, 1)
                else:
                    bury_character(char) # Character is buried and starts suffocating
                
    return effect
```

### Encounter & Mood Resolver (Terrain-Based)

```python
function roll_daily_encounter(terrain: TerrainType, weather: WeatherEffect, party: PartyState) -> String:
    d12_roll = roll_dice(count=1, sides=12) # 1d12
    d6_roll = roll_dice(count=1, sides=6)   # 1d6
    
    total_roll = d12_roll + d6_roll + weather.encounter_modifier
    total_roll = clamp(total_roll, 2, 18)
    mood_roll = d6_roll
    
    if terrain == TerrainType.PILLARS:
        return "Pillars Encounter: Roll on Pillars Encounter table."
        
    elif terrain == TerrainType.RUINS:
        return "Ruins Encounter: Roll on Ruins Encounter table."
        
    # Default: TerrainType.WASTES
    if total_roll <= 5:
        return "No Encounter: The wastes are silent."
    elif total_roll == 6:
        qty = roll_dice(1, 6)
        return f"Lost Travelers ({qty}): Desperate for food/shelter. Helpful if assisted."
    elif total_roll == 7:
        qty = roll_dice(1, 6)
        mood = "Cautious, hostile if disturbed" if mood_roll == 1 else \
               "Curious, peaceful if hailed" if mood_roll <= 4 else \
               "Friendly, gives directions & warns of danger"
        return f"Nomads ({qty}): Mood = {mood}."
    elif total_roll == 8:
        qty = roll_dice(1, 3)
        return f"Merchants ({qty}): Carry supplies on a pulk. Trade Limit: 100g."
    elif total_roll == 9:
        qty = roll_dice(1, 6)
        mood = "Crazed, attacks immediately" if mood_roll <= 2 else \
               "Demands tribute (100 coins or 1 ration per traveler)" if mood_roll <= 5 else \
               "Curious, offers to let party join a raid"
        return f"Bandits ({qty}): Mood = {mood}."
    elif total_roll == 10:
        qty = roll_dice(2, 6)
        faction = get_random_faction()
        return f"Pilgrims ({qty}): Devoted to {faction}. Friendly if joined, hostile if contested."
    elif total_roll == 11:
        qty = roll_dice(1, 6)
        lodestones = roll_dice(1, 20)
        return f"Lodestone Prospectors ({qty}): Transporting {lodestones} lodestones. Hostile if harassed."
    elif total_roll == 12:
        merchants = roll_dice(1, 6)
        nomads = roll_dice(2, 6)
        return f"Caravan ({merchants} Merchants, {nomads} Nomads): Trading pulk. Trade Limit: 1000g."
    elif total_roll == 13:
        qty = roll_dice(1, 6)
        mood = "Crazed, attacks to kill" if mood_roll <= 3 else \
               "Demands tribute (1000 coins or ALL rations)" if mood_roll <= 5 else \
               "Recruit: Demands party members duel; survivor joins."
        return f"Cutthroats ({qty}): Mood = {mood}."
    elif total_roll == 14:
        qty = roll_dice(1, 6)
        return f"Cyclops ({qty}): Clustered for warmth, smelling the air for unwary mortals."
    elif total_roll == 15:
        qty = roll_dice(1, 3)
        return f"Harpies ({qty}): Circling above or hidden under dust."
    elif total_roll == 16:
        qty = roll_dice(1, 3)
        return f"Medusa ({qty}): Curled in hidden spots listening for steps."
    elif total_roll == 17:
        return "Shade: Drifting through the air, vibrating with hunger."
    elif total_roll == 18:
        return "Griffon: Resting on high point. Remains of latest victim are nearby."
```

### Complete Daily Simulation Loop (Movement & Survival)

```python
function simulate_exploration_day(party: PartyState, force_march: bool) -> Log:
    log = new Log()
    
    # --- PHASE 1: TRAVEL SELECTION & WEATHER ---
    # Travelers on foot average 18 miles base or 24 miles if they Forced March (+6 miles)
    base_target_miles = 24 if force_march else 18
    log.append(f"March Plan: {'Forced March' if force_march else 'Normal March'} ({base_target_miles} miles base).")
    
    # Roll daily weather based on current terrain
    weather = roll_daily_weather(party.current_terrain, party)
    log.append(f"Weather in {party.current_terrain}: {weather.type}")
    
    # --- PHASE 2: MOVEMENT CALCULATION & EXHAUSTION ---
    # Calculate net travel progress after weather penalties
    net_travel_miles = max(0, base_target_miles - weather.travel_penalty_miles)
    log.append(f"Net travel progress made: {net_travel_miles} miles.")
    
    if not weather.landmarks_visible:
        log.append("Landmarks are obscured! Navigation is impaired.")
        
    # Apply Forced March penalty (1 level of exhaustion)
    if force_march:
        log.append("Forced March! Travel team pushes past their limits.")
        for char in party.members:
            if char.is_alive:
                apply_exhaustion(char, 1)
                log.append(f"{char.name} gains 1 exhaustion from the Forced March (Current: {char.exhaustion}/6).")

    # --- PHASE 3: ENCOUNTERS & CURIOSITIES ---
    encounter_desc = roll_daily_encounter(party.current_terrain, weather, party)
    log.append(f"Encounter Result: {encounter_desc}")
    
    # Curiosities (e.g. 1-in-6 chance during travel)
    if roll_dice(1, 6) == 1:
        curiosity_desc = roll_curiosity(party)
        log.append(f"Curiosity Spotted: {curiosity_desc}")

    # --- PHASE 4: END-OF-DAY SURVIVAL (RATIONS & REST) ---
    # Starvation check: 1 ration must be consumed per day or suffer a level of exhaustion
    for char in party.members:
        if not char.is_alive:
            continue
            
        if party.rations >= 1:
            party.rations -= 1  # Consume ration
        else:
            apply_exhaustion(char, 1)
            log.append(f"No rations for {char.name}! Suffer 1 exhaustion from starvation (Current: {char.exhaustion}/6).")
            
    # Check for exhaustion casualties (Exhaustion Level 6 = Death)
    for char in party.members:
        if char.is_alive and char.exhaustion >= 6:
            char.is_alive = False
            log.append(f"☠️ {char.name} has died from extreme exhaustion/starvation.")
            
    return log

function apply_exhaustion(char: Character, levels: int):
    char.exhaustion = min(6, char.exhaustion + levels)
```
