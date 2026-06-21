# Roaming Hazards System

This document outlines the mechanics, hazard tables, and algorithmic movement logic for **Roaming Hazards** on the 6-mile Local Scale map (37-subhex grid), as specified in the design rules.

---

## 1. Mechanics & Rules

### A. Initialization (Setup)
1. **Drop Phase**: Roll a D6 to determine the number of hazard dice to drop (exactly **1 to 6 hazards**). Then drop that number of six-sided dice (D6s) randomly onto the 37-subhex local map.
2. **Hazard Presence**: Any subhex containing a die has an active hazard of the type corresponding to the **face-up value** of that die (1–6).
3. **Tracking**: These dice represent mobile, trackable disruptions that persist on the map.

### B. Daily Movement
1. **Movement Roll**: Each game day, roll **1d6** for each active hazard to determine its direction of travel.
2. **Direction Mapping**:
   * The original diagram is designed for pointy-topped hexes (1: North, 2: North-East, 3: South-East, 4: South, 5: South-West, 6: North-West).
   * Translated to our **flat-topped hex grid**, the directions map clockwise starting from North-East:
     
     | D6 Roll | Direction (Flat-Topped) | Coordinate Delta ($\Delta q, \Delta r$) |
     | :---: | :--- | :--- |
     | **1** | North-East | $(+1, -1)$ |
     | **2** | East | $(+1, 0)$ |
     | **3** | South-East | $(0, +1)$ |
     | **4** | South-West | $(-1, +1)$ |
     | **5** | West | $(-1, 0)$ |
     | **6** | North-West | $(0, -1)$ |

### C. Collisions & Boundaries
* **Out of Bounds**: If a hazard moves off the edge of the 37-subhex boundary ($|q|, |r|, |s| > 3$), it is removed and **re-dropped** randomly onto the map.
* **Collision**: If a hazard moves into a subhex already occupied by another hazard (a "bump"), it is **re-dropped** randomly onto an empty subhex.

---

## 2. Roaming Hazards Table (1D6)

| D6 | Hazard | Description & Effects |
| :---: | :--- | :--- |
| **1** | **Warband** | **5d6 Cutthroats** led by a **Demagogue**.<br>• Attacks on sight but will not pursue players into *Ruins* or *Settlements*.<br>• Slaying the Demagogue destroys this hazard.<br>*(See Demagogue stat-block below)* |
| **2** | **Maelstrom** | Columns of violent swirling air.<br>• Characters caught are flung **1 mile** in a random direction and suffer **3d20 damage**.<br>• Can be avoided by hiding inside *Ruins* or sufficiently strong shelter. |
| **3** | **Crawlherd** | **1d20 Crawl** creatures.<br>• A writhing horde that haunts the wastes and ruins.<br>• *Settlements* are safe due to defenses.<br>• Defeating the Crawl herd destroys the hazard. |
| **4** | **Collapse** | Chunk of the ceiling falls from above.<br>• Travelers must gain a level of **exhaustion** running or be crushed.<br>• Any *Ruins* or *Settlements* in the hex have a **2-in-6 chance** of being reduced to *Wastes*. |
| **5** | **Void Lightning** | Jet-black lightning bolts cross the wastes.<br>• Metal-wielding characters have a **3-in-6 chance** of being struck for **10d6 damage**.<br>• Disintegrates those killed into dust.<br>• Avoided by stripping off metal or hiding in *Ruins*. |
| **6** | **Singing Sand** | Quicksand vibrations from deep below.<br>• Non-solid/rocky ground turns to quicksand.<br>• Caught characters must **Save v. Breath** or disappear into the ground.<br>• Avoided by seeking high/solid ground. |

### Demagogue Stat-Block (Warband Leader)
* *An avatar of the inescapable dark, draped in rags and lodestone charms, speaking in a hollow, alien voice.*
* **HD**: 5 | **HP**: 30 | **Move**: Standard
* **Defense**: As Plate | **Weapon**: Lodestone Blade (1d10)
* **Magic**: Knows $1d3$ random spells.
* **Voice of the Dark**: Save vs. Charm when speaking or become frightened.
* **Artifact of Power**: Wields a random artifact (pg. 29).

---

## 3. Algorithmic Pseudo-code

### Data Structures

```python
class Hazard:
    id: String          # Unique identifier
    type: int           # D6 type (1-6)
    coords: HexCoords   # Current position in the local grid
    is_active: bool     # Alive/active state

class LocalGrid:
    subhexes: Dictionary[HexCoords, HexTile] # 37 hexes (radius 3)
    hazards: List[Hazard]

# Delta vectors for flat-topped hex coordinates (clockwise from North-East)
FLAT_TOP_DIRECTIONS = [
    HexCoords(1, -1),  # 1: North-East
    HexCoords(1, 0),   # 2: East
    HexCoords(0, 1),   # 3: South-East
    HexCoords(-1, 1),  # 4: South-West
    HexCoords(-1, 0),  # 5: West
    HexCoords(0, -1)   # 6: North-West
]
```

### Setup & Re-drop Logic

```python
function initialize_roaming_hazards(grid: LocalGrid):
    # Roll 1d6 to determine the number of hazard dice to drop (1 to 6)
    num_hazards = roll_d6()
    
    for i from 1 to num_hazards:
        # Roll a D6 to determine the type of hazard (1 to 6)
        hazard_type = roll_d6()
        hazard = new Hazard(id=generate_uuid(), type=hazard_type)
        drop_hazard_randomly(grid, hazard)
        grid.hazards.append(hazard)

function drop_hazard_randomly(grid: LocalGrid, hazard: Hazard):
    # Find all empty subhexes not occupied by other hazards
    eligible_coords = []
    for coords in grid.subhexes.keys():
        occupied = false
        for h in grid.hazards:
            if h.is_active and h.coords == coords:
                occupied = true
                break
        if not occupied:
            eligible_coords.append(coords)
            
    if length(eligible_coords) > 0:
        # Assign to a random eligible subhex
        hazard.coords = random_choice(eligible_coords)
        hazard.is_active = true
    else:
        # Fallback if no space
        hazard.is_active = false
```

### Daily Update & Movement Loop

```python
function update_hazards_daily(grid: LocalGrid):
    for hazard in grid.hazards:
        if not hazard.is_active:
            continue
            
        # 1. Roll 1d6 for movement direction (1-based index)
        move_roll = roll_d6()
        direction_vector = FLAT_TOP_DIRECTIONS[move_roll - 1]
        
        # 2. Compute target coordinate
        target_coords = HexCoords(
            hazard.coords.q + direction_vector.q,
            hazard.coords.r + direction_vector.r
        )
        
        # 3. Check boundaries (radius 3 constraint: |q| <= 3, |r| <= 3, |q+r| <= 3)
        if abs(target_coords.q) > 3 or abs(target_coords.r) > 3 or abs(target_coords.q + target_coords.r) > 3:
            # Off edge: Re-drop
            drop_hazard_randomly(grid, hazard)
            continue
            
        # 4. Check for collision with another active hazard
        collision_detected = false
        for other in grid.hazards:
            if other.id != hazard.id and other.is_active and other.coords == target_coords:
                collision_detected = true
                break
                
        if collision_detected:
            # Bump: Re-drop
            drop_hazard_randomly(grid, hazard)
        else:
            # Move successful
            hazard.coords = target_coords
```
