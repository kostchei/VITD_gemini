using Godot;
using System;
using System.Collections.Generic;
using VastDark.Common;
using VastDark.Models;
using VastDark.UI;
using VastDark.View;

namespace VastDark;

public partial class Main : Node2D
{
    private MapData _mapData = null!;
    private MapRenderer _renderer = null!;
    private CameraController _camera = null!;
    private GameUI _ui = null!;
    private PartyState _partyState = null!;

    public override void _Ready()
    {
        // 1. Generate World Data
        _mapData = Generation.MockGenerator.GenerateWorld();

        // Initialize Party State
        _partyState = new PartyState();

        // 2. Pre-generate all local maps upfront (so the entire world state is fully created)
        foreach (var regTile in _mapData.RegionalTiles.Values)
        {
            var localMap = _mapData.GetOrCreateLocalMap(regTile.Coords, (rc) => 
                Generation.MockGenerator.GenerateLocalMap(rc, regTile)
            );
            _mapData.GetOrCreateLocalHazards(regTile.Coords, (rc) =>
                Generation.MockGenerator.GenerateLocalHazards(rc, localMap)
            );
        }

        // 3. Save the complete world state to files
        Generation.MapSerializer.SaveToFile(_mapData, "world_map.json");
        try
        {
            string globalUserPath = ProjectSettings.GlobalizePath("user://world_map.json");
            Generation.MapSerializer.SaveToFile(_mapData, globalUserPath);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save to user path: {ex.Message}");
        }

        // 4. Cache Node References
        _renderer = GetNode<MapRenderer>("MapRenderer");
        _camera = GetNode<CameraController>("CameraController");
        _ui = GetNode<GameUI>("GameUI");

        // 5. Initialize Map Renderer
        _renderer.Initialize(_mapData);

        // 4. Connect Signals from Renderer
        _renderer.RegionalSelected += OnRegionalSelected;
        _renderer.LocalSelected += OnLocalSelected;
        _renderer.DungeonSelected += OnDungeonSelected;

        // 5. Connect Signals from UI
        _ui.BreadcrumbPressed += OnBreadcrumbPressed;
        _ui.DungeonLevelChanged += OnDungeonLevelChanged;
        _ui.CameraControlPressed += OnCameraControlPressed;
        _ui.ActionButtonPressed += OnActionButtonPressed;
        _ui.RegenPressed += OnRegenPressed;
        _ui.NextDayPressed += OnNextDayPressed;
        _ui.ForcedMarchToggled += OnForcedMarchToggled;
        _ui.RecenterPressed += OnRecenterPressed;

        // 6. Set Initial State
        EnterRegionalScale();
        UpdatePartyStatusUI();

        // 7. Check for test-run argument
        string[] args = OS.GetCmdlineArgs();
        bool isTestRun = false;
        foreach (var arg in args)
        {
            if (arg == "--test-run")
            {
                isTestRun = true;
                break;
            }
        }

        if (isTestRun)
        {
            RunAutomatedTest();
        }
    }

    private void OnRegionalSelected(int q, int r)
    {
        var coords = new HexCoords(q, r);
        if (_mapData.RegionalTiles.TryGetValue(coords, out var tile))
        {
            _ui.UpdateUIState(
                MapScale.Regional,
                tile.Landmark == "None" ? null : tile.Name,
                $"Q: {q}, R: {r}",
                tile.Biome,
                1
            );
        }
        else
        {
            _ui.UpdateUIState(MapScale.Regional, null, null, null, 1);
        }
    }

    private void OnLocalSelected(int q, int r)
    {
        if (!_renderer.SelectedRegCoords.HasValue) return;
        var parentCoords = _renderer.SelectedRegCoords.Value;
        var parentTile = _mapData.RegionalTiles[parentCoords];
        var localMap = _mapData.GetOrCreateLocalMap(parentCoords, (rc) => 
            Generation.MockGenerator.GenerateLocalMap(rc, parentTile)
        );
        var hazards = _mapData.GetOrCreateLocalHazards(parentCoords, (rc) =>
            Generation.MockGenerator.GenerateLocalHazards(rc, localMap)
        );

        var coords = new HexCoords(q, r);

        // Check if coords is an out-of-bounds adjacent exit hex
        bool isExitHex = Math.Abs(q) > 3 || Math.Abs(r) > 3 || Math.Abs(coords.S) > 3;

        // Check adjacency for movement
        int distance = _partyState.LocalCoords.DistanceTo(coords);
        if (distance == 1)
        {
            if (isExitHex)
            {
                // Edge Transition!
                HexCoords direction = new HexCoords(coords.Q - _partyState.LocalCoords.Q, coords.R - _partyState.LocalCoords.R);
                HexCoords newRegCoords = new HexCoords(parentCoords.Q + direction.Q, parentCoords.R + direction.R);

                if (_mapData.RegionalTiles.TryGetValue(newRegCoords, out var newRegTile))
                {
                    // Check daily limit
                    if (_partyState.MilesTraveledToday >= _partyState.DailyMovementLimit)
                    {
                        _partyState.DailyLog = $"⚠️ MOVEMENT BLOCKED: The party is exhausted from traveling {_partyState.DailyMovementLimit} miles today!\n" +
                                               $"Click 'Next Day' to rest and regain movement budget.";
                        UpdatePartyStatusUI();
                    }
                    else
                    {
                        // Calculate entering local coordinates
                        HexCoords newLocalCoords = new HexCoords(coords.Q - 6 * direction.Q, coords.R - 6 * direction.R);

                        // Load target local map to ensure it exists
                        var newLocalMap = _mapData.GetOrCreateLocalMap(newRegCoords, (rc) => 
                            Generation.MockGenerator.GenerateLocalMap(rc, newRegTile)
                        );
                        var newHazards = _mapData.GetOrCreateLocalHazards(newRegCoords, (rc) =>
                            Generation.MockGenerator.GenerateLocalHazards(rc, newLocalMap)
                        );

                        // Move party state coordinates
                        _partyState.LocalCoords = newLocalCoords;
                        _renderer.PartyLocalCoords = newLocalCoords;

                        // Increment miles
                        _partyState.MilesTraveledToday += 1;
                        _partyState.TotalMilesTraveled += 1;
                        _partyState.MilesSinceLastEncounterCheck += 1;

                        string logMsg = $"Party traveled off the edge into adjacent regional hex {newRegTile.Name}!\n" +
                                        $"Entered new region at subhex {newLocalCoords}.\n" +
                                        $"Miles Traveled Today: {_partyState.MilesTraveledToday}/{_partyState.DailyMovementLimit} miles.\n" +
                                        $"Total Miles Traveled: {_partyState.TotalMilesTraveled} miles.";

                        // Check if we hit the 6-mile encounter check threshold
                        if (_partyState.MilesSinceLastEncounterCheck >= 6)
                        {
                            _partyState.MilesSinceLastEncounterCheck = 0;
                            int d12Roll = RollDice(1, 12);
                            int d6Roll = RollDice(1, 6);
                            int totalEncounterRoll = Math.Clamp(d12Roll + d6Roll + _partyState.ActiveWeatherEncounterMod, 2, 18);
                            int moodRoll = d6Roll;

                            string encounterDesc = ResolveEncounterDescription(totalEncounterRoll, moodRoll);
                            logMsg += $"\n\n⚠️ ENCOUNTER TRIGGERED (Moved 6 miles)!\n{encounterDesc}";
                        }

                        _partyState.DailyLog = logMsg;

                        // Check for hazard collision in the new region!
                        CheckAndResolveHazardCollision(newLocalCoords, newHazards, newLocalMap);

                        // Transition scale view to the new region
                        EnterLocalScale(newRegCoords);

                        // Force update selection to our new coordinate in the new local map!
                        _renderer.SelectedLocalCoords = newLocalCoords;
                        OnLocalSelected(newLocalCoords.Q, newLocalCoords.R);
                        return;
                    }
                }
                else
                {
                    _partyState.DailyLog = "⚠️ The wastes extend forever in this direction. There is no mapping beyond this boundary!";
                    UpdatePartyStatusUI();
                }
            }
            else
            {
                // Normal local move (inside the 37-subhex grid)
                // Check daily limit
                if (_partyState.MilesTraveledToday >= _partyState.DailyMovementLimit)
                {
                    _partyState.DailyLog = $"⚠️ MOVEMENT BLOCKED: The party is exhausted from traveling {_partyState.DailyMovementLimit} miles today!\n" +
                                           $"Click 'Next Day' to rest and regain movement budget.";
                    UpdatePartyStatusUI();
                }
                else
                {
                    // Move party
                    _partyState.LocalCoords = coords;
                    _renderer.PartyLocalCoords = coords;
                    
                    // Increment miles
                    _partyState.MilesTraveledToday += 1;
                    _partyState.TotalMilesTraveled += 1;
                    _partyState.MilesSinceLastEncounterCheck += 1;

                    string logMsg = $"Party moved to subhex {coords} (1 mile traveled).\n" +
                                    $"Miles Traveled Today: {_partyState.MilesTraveledToday}/{_partyState.DailyMovementLimit} miles.\n" +
                                    $"Total Miles Traveled: {_partyState.TotalMilesTraveled} miles.";

                    // Check if we hit the 6-mile encounter check threshold
                    if (_partyState.MilesSinceLastEncounterCheck >= 6)
                    {
                        _partyState.MilesSinceLastEncounterCheck = 0;
                        
                        int d12Roll = RollDice(1, 12);
                        int d6Roll = RollDice(1, 6);
                        int totalEncounterRoll = Math.Clamp(d12Roll + d6Roll + _partyState.ActiveWeatherEncounterMod, 2, 18);
                        int moodRoll = d6Roll;

                        string encounterDesc = ResolveEncounterDescription(totalEncounterRoll, moodRoll);
                        logMsg += $"\n\n⚠️ ENCOUNTER TRIGGERED (Moved 6 miles)!\n{encounterDesc}";
                    }

                    _partyState.DailyLog = logMsg;
                    CheckAndResolveHazardCollision(coords, hazards, localMap);
                    UpdatePartyStatusUI();
                    _renderer.QueueRedraw();
                }
            }
        }

        // Inspector state details updating
        if (isExitHex)
        {
            HexCoords direction = new HexCoords(coords.Q - _partyState.LocalCoords.Q, coords.R - _partyState.LocalCoords.R);
            HexCoords adjRegCoords = new HexCoords(parentCoords.Q + direction.Q, parentCoords.R + direction.R);
            
            if (_mapData.RegionalTiles.TryGetValue(adjRegCoords, out var adjRegTile))
            {
                _ui.UpdateUIState(
                    MapScale.Local,
                    $"Exit to {adjRegTile.Name}",
                    $"Q: {q}, R: {r}",
                    "Border Crossing",
                    1,
                    $"Moving here will transition the party to the adjacent regional hex: {adjRegTile.Name}."
                );
            }
            else
            {
                _ui.UpdateUIState(
                    MapScale.Local,
                    "The Unknown Edge",
                    $"Q: {q}, R: {r}",
                    "Uncharted Wastes",
                    1,
                    "The wastes extend endlessly in this direction. There is no mapped route."
                );
            }
        }
        else if (localMap.TryGetValue(coords, out var tile))
        {
            // Find if there is an active hazard on this subhex
            Hazard? activeHazard = null;
            foreach (var h in hazards)
            {
                if (h.IsActive && h.Coords == coords)
                {
                    activeHazard = h;
                    break;
                }
            }

            string? selectedName = tile.Landmark == "None" ? null : tile.Name;
            string? customDetails = null;

            if (activeHazard != null)
            {
                selectedName = selectedName != null 
                    ? $"{selectedName} (HAZARD: {activeHazard.Name})" 
                    : $"HAZARD: {activeHazard.Name}";
                customDetails = $"{activeHazard.Name}: {activeHazard.Description}";
            }

            _ui.UpdateUIState(
                MapScale.Local,
                selectedName,
                $"Q: {q}, R: {r}",
                tile.Biome,
                1,
                customDetails
            );
        }
        else
        {
            _ui.UpdateUIState(MapScale.Local, null, null, null, 1);
        }
    }

    private void OnDungeonSelected(int x, int y)
    {
        if (!_renderer.SelectedRegCoords.HasValue || !_renderer.SelectedLocalCoords.HasValue) return;
        var levels = _mapData.GetOrCreateDungeon(_renderer.SelectedRegCoords.Value, _renderer.SelectedLocalCoords.Value, () => 
            Generation.MockGenerator.GenerateDungeonLevels()
        );
        var level = levels[_renderer.ActiveDungeonLevel - 1];

        if (x >= 0 && x < level.Width && y >= 0 && y < level.Height)
        {
            var cellType = level.Cells[x, y];
            string biomeText = cellType.ToString();
            string landmarkText = cellType switch
            {
                DungeonCellType.StairsUp => "Stairs to upper floor",
                DungeonCellType.StairsDown => "Stairs to lower floor",
                DungeonCellType.Chest => "Loot Chest",
                DungeonCellType.Encounter => "Hostile Encounter",
                DungeonCellType.Door => "Heavy Door",
                DungeonCellType.Room => "Chamber Room",
                DungeonCellType.Corridor => "Stone Corridor",
                DungeonCellType.Wall => "Solid Wall",
                _ => "Empty Space"
            };

            _ui.UpdateUIState(
                MapScale.Dungeon,
                landmarkText,
                $"X: {x}, Y: {y}",
                biomeText,
                _renderer.ActiveDungeonLevel
            );
        }
        else
        {
            _ui.UpdateUIState(MapScale.Dungeon, null, null, null, _renderer.ActiveDungeonLevel);
        }
    }

    private void OnBreadcrumbPressed(int targetScale)
    {
        var scale = (MapScale)targetScale;
        if (scale == MapScale.Regional)
        {
            EnterRegionalScale();
        }
        else if (scale == MapScale.Local)
        {
            if (_renderer.SelectedRegCoords.HasValue)
            {
                EnterLocalScale(_renderer.SelectedRegCoords.Value);
            }
        }
        else if (scale == MapScale.Dungeon)
        {
            if (_renderer.SelectedRegCoords.HasValue && _renderer.SelectedLocalCoords.HasValue)
            {
                EnterDungeonScale(_renderer.SelectedRegCoords.Value, _renderer.SelectedLocalCoords.Value, _renderer.ActiveDungeonLevel);
            }
        }
    }

    private void OnDungeonLevelChanged(int floor)
    {
        _renderer.SetDungeonLevel(floor);
        _ui.UpdateUIState(
            MapScale.Dungeon,
            null,
            null,
            null,
            floor
        );
    }

    private void OnCameraControlPressed(string command)
    {
        switch (command)
        {
            case "in":
                _camera.CenterOn(_camera.Position, _camera.Zoom.X * 1.25f);
                break;
            case "out":
                _camera.CenterOn(_camera.Position, _camera.Zoom.X * 0.8f);
                break;
            case "reset":
                ResetCameraForScale(_renderer.CurrentScale);
                break;
        }
    }

    private void OnRegenPressed()
    {
        GD.Print("[Main] Regenerating new map...");
        
        _partyState = new PartyState();
        UpdatePartyStatusUI();
        
        // 1. Generate new World Data
        _mapData = Generation.MockGenerator.GenerateWorld();

        // 2. Pre-generate all local maps upfront (so the entire world state is fully created)
        foreach (var regTile in _mapData.RegionalTiles.Values)
        {
            var localMap = _mapData.GetOrCreateLocalMap(regTile.Coords, (rc) => 
                Generation.MockGenerator.GenerateLocalMap(rc, regTile)
            );
            _mapData.GetOrCreateLocalHazards(regTile.Coords, (rc) =>
                Generation.MockGenerator.GenerateLocalHazards(rc, localMap)
            );
        }

        // 3. Save the complete world state to files
        Generation.MapSerializer.SaveToFile(_mapData, "world_map.json");
        try
        {
            string globalUserPath = ProjectSettings.GlobalizePath("user://world_map.json");
            Generation.MapSerializer.SaveToFile(_mapData, globalUserPath);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save to user path: {ex.Message}");
        }

        // 4. Re-initialize Map Renderer and reset selection state
        _renderer.Initialize(_mapData);
        _renderer.SelectedRegCoords = null;
        _renderer.HoveredRegCoords = null;
        _renderer.SelectedLocalCoords = null;
        _renderer.HoveredLocalCoords = null;
        _renderer.SelectedDungeonCell = null;
        _renderer.HoveredDungeonCell = null;

        // 5. Return to Regional Scale
        EnterRegionalScale();
    }

    private void OnActionButtonPressed()
    {
        switch (_renderer.CurrentScale)
        {
            case MapScale.Regional:
                if (_renderer.SelectedRegCoords.HasValue)
                {
                    EnterLocalScale(_renderer.SelectedRegCoords.Value);
                }
                break;

            case MapScale.Local:
                if (_renderer.SelectedRegCoords.HasValue && _renderer.SelectedLocalCoords.HasValue)
                {
                    // Check if selected subhex contains a dungeon entrance
                    var parentTile = _mapData.RegionalTiles[_renderer.SelectedRegCoords.Value];
                    var localMap = _mapData.GetOrCreateLocalMap(_renderer.SelectedRegCoords.Value, (rc) => 
                        Generation.MockGenerator.GenerateLocalMap(rc, parentTile)
                    );
                    
                    if (localMap.TryGetValue(_renderer.SelectedLocalCoords.Value, out var subTile) && 
                        subTile.Landmark.Contains("Dungeon"))
                    {
                        EnterDungeonScale(_renderer.SelectedRegCoords.Value, _renderer.SelectedLocalCoords.Value);
                    }
                }
                break;

            case MapScale.Dungeon:
                // Exit dungeon back to local scale
                if (_renderer.SelectedRegCoords.HasValue)
                {
                    EnterLocalScale(_renderer.SelectedRegCoords.Value);
                }
                break;
        }
    }

    private void EnterRegionalScale()
    {
        // Smooth transition fade-out/fade-in
        var tween = CreateTween();
        tween.TweenProperty(_renderer, "modulate:a", 0.0f, 0.12f);
        tween.TweenCallback(Callable.From(() =>
        {
            _renderer.SetScale(MapScale.Regional);
            _camera.SetLimits(new Rect2(-300, -250, 1900, 1400));
            ResetCameraForScale(MapScale.Regional);

            if (_renderer.SelectedRegCoords.HasValue)
            {
                OnRegionalSelected(_renderer.SelectedRegCoords.Value.Q, _renderer.SelectedRegCoords.Value.R);
            }
            else
            {
                _ui.UpdateUIState(MapScale.Regional, null, null, null, 1);
            }
        }));
        tween.TweenProperty(_renderer, "modulate:a", 1.0f, 0.12f);
    }

    private void OnRecenterPressed()
    {
        if (_renderer.CurrentScale == MapScale.Local)
        {
            Vector2 partyCenter = _partyState.LocalCoords.ToPixel(_renderer.LocalHexSize, true);
            // Adjust a little left to offset right HUD panel
            partyCenter.X -= 60;
            _camera.CenterOn(partyCenter, 0.8f);
        }
        else if (_renderer.CurrentScale == MapScale.Dungeon)
        {
            _camera.CenterOn(new Vector2(-60, 0), 1.1f);
        }
    }

    private void EnterLocalScale(HexCoords regCoords)
    {
        var tween = CreateTween();
        tween.TweenProperty(_renderer, "modulate:a", 0.0f, 0.12f);
        tween.TweenCallback(Callable.From(() =>
        {
            _renderer.SetScale(MapScale.Local, regCoords);
            _camera.SetLimits(new Rect2(-2500, -2000, 5000, 4000));
            ResetCameraForScale(MapScale.Local);

            if (_renderer.SelectedLocalCoords.HasValue)
            {
                OnLocalSelected(_renderer.SelectedLocalCoords.Value.Q, _renderer.SelectedLocalCoords.Value.R);
            }
            else
            {
                _ui.UpdateUIState(MapScale.Local, null, null, null, 1);
            }
            UpdatePartyStatusUI();
        }));
        tween.TweenProperty(_renderer, "modulate:a", 1.0f, 0.12f);
    }

    private void EnterDungeonScale(HexCoords regCoords, HexCoords localCoords, int floor = 1)
    {
        var tween = CreateTween();
        tween.TweenProperty(_renderer, "modulate:a", 0.0f, 0.12f);
        tween.TweenCallback(Callable.From(() =>
        {
            _renderer.SetScale(MapScale.Dungeon, regCoords, localCoords, floor);
            _camera.SetLimits(new Rect2(-700, -500, 1400, 1000));
            ResetCameraForScale(MapScale.Dungeon);

            if (_renderer.SelectedDungeonCell.HasValue)
            {
                OnDungeonSelected(_renderer.SelectedDungeonCell.Value.X, _renderer.SelectedDungeonCell.Value.Y);
            }
            else
            {
                _ui.UpdateUIState(MapScale.Dungeon, null, null, null, floor);
            }
        }));
        tween.TweenProperty(_renderer, "modulate:a", 1.0f, 0.12f);
    }

    private void ResetCameraForScale(MapScale scale)
    {
        switch (scale)
        {
            case MapScale.Regional:
                // Center camera on the regional grid center
                Vector2 center = HexCoords.FromOffset(MapData.RegWidth / 2, MapData.RegHeight / 2).ToPixel(_renderer.RegHexSize, true);
                // Adjust a little left to offset right HUD panel
                center.X -= 50; 
                _camera.CenterOn(center, 0.65f);
                break;

            case MapScale.Local:
                // Center on the party's current position
                Vector2 partyPixel = _partyState.LocalCoords.ToPixel(_renderer.LocalHexSize, true);
                partyPixel.X -= 60;
                _camera.CenterOn(partyPixel, 0.8f);
                break;

            case MapScale.Dungeon:
                // Center on dungeon grid origin (0, 0)
                _camera.CenterOn(new Vector2(-60, 0), 1.1f);
                break;
        }
    }

    private async void RunAutomatedTest()
    {
        GD.Print("[TEST] Starting automated integration test...");
        string artifactDir = System.Environment.GetEnvironmentVariable("GEMINI_ARTIFACT_DIR") 
            ?? "C:/Users/Admin/.gemini/antigravity/brain/b7ef128f-c031-437d-97cc-80f5a35be685";
        
        // Wait 1.5 seconds for UI and drawing to settle
        await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        
        // State 1: Regional Map
        GD.Print("[TEST] Capturing Regional Map...");
        var regCoords = HexCoords.FromOffset(3, 2);
        _renderer.SelectedRegCoords = regCoords;
        OnRegionalSelected(regCoords.Q, regCoords.R);
        await CaptureScreenshot(artifactDir + "/test_regional.png");
        
        // State 2: Select Dungeon Hex at (3,2) and Zoom to Local Map
        GD.Print("[TEST] Selecting Dungeon hex at (3,2) and entering Local scale...");
        EnterLocalScale(regCoords);
        
        await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        GD.Print("[TEST] Capturing Local Map...");
        var localCoords = new HexCoords(0, 0);
        _renderer.SelectedLocalCoords = localCoords;
        OnLocalSelected(localCoords.Q, localCoords.R);
        await CaptureScreenshot(artifactDir + "/test_local.png");
        
        // State 3: Enter Dungeon Map
        GD.Print("[TEST] Entering Dungeon Scale...");
        EnterDungeonScale(regCoords, localCoords);
        
        await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        GD.Print("[TEST] Capturing Dungeon Floor -1 Map...");
        await CaptureScreenshot(artifactDir + "/test_dungeon_l1.png");
        
        // State 4: Switch to Dungeon Floor -3
        GD.Print("[TEST] Switching to Dungeon Floor -3...");
        OnDungeonLevelChanged(3);
        
        await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        GD.Print("[TEST] Capturing Dungeon Floor -3 Map...");
        await CaptureScreenshot(artifactDir + "/test_dungeon_l3.png");
        
        GD.Print("[TEST] Integration test complete. Exiting...");
        GetTree().Quit();
    }

    private async System.Threading.Tasks.Task CaptureScreenshot(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = GetViewport().GetTexture().GetImage();
        if (image != null)
        {
            Error err = image.SavePng(path);
            if (err == Error.Ok)
            {
                GD.Print($"[TEST] Saved screenshot to: {path}");
            }
            else
            {
                GD.PrintErr($"[TEST] Failed to save screenshot. Error: {err}");
            }
        }
    }

    private void OnNextDayPressed()
    {
        AdvanceDay();
    }

    private void OnForcedMarchToggled(bool pressed)
    {
        if (pressed)
        {
            if (!_partyState.ForcedMarchActive)
            {
                _partyState.ForcedMarchActive = true;
                _partyState.DailyMovementLimit += 6;
                
                var log = new System.Text.StringBuilder(_partyState.DailyLog);
                log.AppendLine("\n• Forced March activated! Party gains 1 exhaustion level.");
                foreach (var member in _partyState.Members)
                {
                    if (member.IsAlive)
                    {
                        member.Exhaustion = Math.Min(6, member.Exhaustion + 1);
                        log.AppendLine($"  - {member.Name} gains 1 exhaustion (Exh: {member.Exhaustion}/6).");
                    }
                }
                
                // Check exhaustion deaths immediately
                foreach (var member in _partyState.Members)
                {
                    if (member.IsAlive && member.Exhaustion >= 6)
                    {
                        member.IsAlive = false;
                        member.HP = 0;
                        log.AppendLine($"☠️ {member.Name} has died of exhaustion!");
                    }
                }

                _partyState.DailyLog = log.ToString();
                UpdatePartyStatusUI();
            }
        }
        else
        {
            if (_partyState.ForcedMarchActive)
            {
                // Can only untoggle if they haven't traveled beyond the normal limit
                // The normal limit is DailyMovementLimit - 6
                if (_partyState.MilesTraveledToday > _partyState.DailyMovementLimit - 6)
                {
                    _ui.SetForcedMarchToggled(true);
                    var log = new System.Text.StringBuilder(_partyState.DailyLog);
                    log.AppendLine("\n⚠️ Cannot cancel Forced March: Party has already traveled too far today!");
                    _partyState.DailyLog = log.ToString();
                    UpdatePartyStatusUI();
                    return;
                }
                
                _partyState.ForcedMarchActive = false;
                _partyState.DailyMovementLimit -= 6;
                
                // Remove 1 level of exhaustion added by forced march
                var logMsg = new System.Text.StringBuilder(_partyState.DailyLog);
                logMsg.AppendLine("\n• Forced March cancelled. Party recovers 1 exhaustion level.");
                foreach (var member in _partyState.Members)
                {
                    if (member.IsAlive)
                    {
                        member.Exhaustion = Math.Max(0, member.Exhaustion - 1);
                        logMsg.AppendLine($"  - {member.Name} recovers 1 exhaustion (Exh: {member.Exhaustion}/6).");
                    }
                }
                
                _partyState.DailyLog = logMsg.ToString();
                UpdatePartyStatusUI();
            }
        }
    }

    private void AdvanceDay()
    {
        if (!_renderer.SelectedRegCoords.HasValue) return;
        var regCoords = _renderer.SelectedRegCoords.Value;

        var parentTile = _mapData.RegionalTiles[regCoords];
        var localMap = _mapData.GetOrCreateLocalMap(regCoords, (rc) => 
            Generation.MockGenerator.GenerateLocalMap(rc, parentTile)
        );
        var hazards = _mapData.GetOrCreateLocalHazards(regCoords, (rc) =>
            Generation.MockGenerator.GenerateLocalHazards(rc, localMap)
        );

        // Run Daily Exploration & Survival Simulation
        RunDailySimulation(parentTile);

        var random = new Random();

        foreach (var hazard in hazards)
        {
            if (!hazard.IsActive) continue;

            // 1. Roll 1d6 for movement direction (1-based index)
            int moveRoll = random.Next(1, 7);
            var directionVector = FlatTopDirections[moveRoll - 1];

            // 2. Compute target coordinate
            var targetCoords = new HexCoords(
                hazard.Coords.Q + directionVector.Q,
                hazard.Coords.R + directionVector.R
            );

            // 3. Check boundaries (radius 3 constraint: |q| <= 3, |r| <= 3, |q+r| <= 3)
            if (Math.Abs(targetCoords.Q) > 3 || Math.Abs(targetCoords.R) > 3 || Math.Abs(targetCoords.S) > 3)
            {
                // Off edge: Re-drop
                Generation.MockGenerator.DropHazardRandomly(localMap, hazards, hazard);
                continue;
            }

            // 4. Check for collision with another active hazard
            bool collisionDetected = false;
            foreach (var other in hazards)
            {
                if (other.Id != hazard.Id && other.IsActive && other.Coords == targetCoords)
                {
                    collisionDetected = true;
                    break;
                }
            }

            if (collisionDetected)
            {
                // Bump: Re-drop
                Generation.MockGenerator.DropHazardRandomly(localMap, hazards, hazard);
            }
            else
            {
                // Move successful
                hazard.Coords = targetCoords;
            }

            // Check if hazard moved onto the party
            if (hazard.IsActive && hazard.Coords == _partyState.LocalCoords)
            {
                CheckAndResolveHazardCollision(_partyState.LocalCoords, hazards, localMap);
            }
        }

        // Force redraw to update graphics
        _renderer.QueueRedraw();

        // Update UI details for selected tile in case hazard moved onto or off of it
        if (_renderer.SelectedLocalCoords.HasValue)
        {
            OnLocalSelected(_renderer.SelectedLocalCoords.Value.Q, _renderer.SelectedLocalCoords.Value.R);
        }

        UpdatePartyStatusUI();
    }

    private void UpdatePartyStatusUI()
    {
        var sbStatus = new System.Text.StringBuilder();
        sbStatus.AppendLine("--- PARTY VITALS ---");
        sbStatus.AppendLine($"Rations: {_partyState.Rations}  |  Gold: {_partyState.Gold}g");
        sbStatus.AppendLine($"Miles Today: {_partyState.MilesTraveledToday}/{_partyState.DailyMovementLimit}");
        sbStatus.AppendLine($"Total Miles: {_partyState.TotalMilesTraveled}");
        sbStatus.AppendLine("Members:");
        foreach (var member in _partyState.Members)
        {
            string status = member.IsAlive 
                ? $"HP: {member.HP}/{member.MaxHP} (Exh: {member.Exhaustion}/6)" 
                : "☠️ DECEASED";
            sbStatus.AppendLine($"- {member.Name}: {status}");
        }
        
        _ui.UpdatePartyUI(sbStatus.ToString(), _partyState.DailyLog);
    }

    private void RunDailySimulation(HexTile parentTile)
    {
        var random = new Random();
        var log = new System.Text.StringBuilder();
        log.AppendLine("--- DAY PROGRESS ---");

        // 1. Weather Phase
        _partyState.ActiveWeatherEncounterMod = 0;
        string weatherType = "Calm";
        int weatherTravelPenalty = 0;
        bool landmarksVisible = true;

        if (parentTile.Biome == "Pillars")
        {
            weatherType = "Calm (Column Shelter)";
        }
        else if (parentTile.Biome == "Ruins")
        {
            weatherType = "Calm (Ruins Windbreak)";
        }
        else // Wastes
        {
            int weatherRoll = RollDice(2, 6);
            if (weatherRoll <= 6)
            {
                weatherType = "Calm";
            }
            else if (weatherRoll == 7)
            {
                weatherType = "Dust Storm";
                weatherTravelPenalty = 6;
                landmarksVisible = false;
            }
            else if (weatherRoll == 8)
            {
                weatherType = "Wind Blast";
                log.AppendLine("• Vicious wind blasts the party!");
                if (!_partyState.Sheltered)
                {
                    foreach (var member in _partyState.Members)
                    {
                        if (!member.IsAlive) continue;
                        if (random.Next(1, 21) < 10) // Save vs Breath (DC 10)
                        {
                            int dmg = RollDice(3, 6);
                            member.HP = Math.Max(0, member.HP - dmg);
                            log.AppendLine($"  - {member.Name} failed save, took {dmg} damage (HP: {member.HP}/{member.MaxHP}).");
                        }
                        else
                        {
                            log.AppendLine($"  - {member.Name} saved successfully.");
                        }
                    }
                }
            }
            else if (weatherRoll == 9)
            {
                weatherType = "Stone Hail";
                log.AppendLine("• Cavern roof stone hail rains down!");
                if (!_partyState.Sheltered)
                {
                    foreach (var member in _partyState.Members)
                    {
                        if (!member.IsAlive) continue;
                        if (random.Next(1, 21) < 10) // Save vs Breath (DC 10)
                        {
                            int dmg = RollDice(3, 6);
                            member.HP = Math.Max(0, member.HP - dmg);
                            log.AppendLine($"  - {member.Name} failed save, took {dmg} damage (HP: {member.HP}/{member.MaxHP}).");
                        }
                        else
                        {
                            log.AppendLine($"  - {member.Name} saved successfully.");
                        }
                    }
                }
            }
            else if (weatherRoll == 10)
            {
                weatherType = "Pillar Fog";
                _partyState.ActiveWeatherEncounterMod = 6;
                landmarksVisible = false;
            }
            else if (weatherRoll == 11)
            {
                weatherType = "Grit Slide";
                weatherTravelPenalty = 6;
                log.AppendLine("• Grit Slide shifts the dunes!");
                if (!_partyState.Sheltered)
                {
                    foreach (var member in _partyState.Members)
                    {
                        if (!member.IsAlive) continue;
                        if (random.Next(1, 21) < 10) // Save vs Breath (DC 10)
                        {
                            int dmg = RollDice(3, 6);
                            member.HP = Math.Max(0, member.HP - dmg);
                            log.AppendLine($"  - {member.Name} failed save, took {dmg} damage (HP: {member.HP}/{member.MaxHP}).");
                        }
                        else
                        {
                            log.AppendLine($"  - {member.Name} saved successfully.");
                        }
                    }
                }
            }
            else if (weatherRoll == 12)
            {
                weatherType = "Dune Wave";
                log.AppendLine("• Massive Dune Wave collapses from below!");
                foreach (var member in _partyState.Members)
                {
                    if (!member.IsAlive) continue;
                    if (random.Next(1, 21) >= 10) // Save vs Breath/Reflex (DC 10)
                    {
                        member.Exhaustion = Math.Min(6, member.Exhaustion + 1);
                        log.AppendLine($"  - {member.Name} fled, gaining 1 exhaustion (Exh: {member.Exhaustion}/6).");
                    }
                    else
                    {
                        member.Exhaustion = Math.Min(6, member.Exhaustion + 2); // Buried/Extreme stress
                        log.AppendLine($"  - {member.Name} was buried, gaining 2 exhaustion (Exh: {member.Exhaustion}/6).");
                    }
                }
            }
        }
        log.AppendLine($"Weather: {weatherType}");

        // Save forced march status of the day that just ended, then reset it for the new day
        bool wasForcedMarch = _partyState.ForcedMarchActive;
        _partyState.ForcedMarchActive = false;
        _ui.SetForcedMarchToggled(false);

        // 2. Travel & Movement Phase
        int baseLimit = 18;
        _partyState.DailyMovementLimit = Math.Max(0, baseLimit - weatherTravelPenalty);
        _partyState.MilesTraveledToday = 0; // Reset daily counter on rest/new day
        log.AppendLine($"Today's Movement Limit: {_partyState.DailyMovementLimit} miles.");

        if (!landmarksVisible)
        {
            log.AppendLine("• Landmarks obscured! Danger of getting lost.");
        }

        // 3. Encounter Phase
        string encounterResult = "Encounters will occur as you travel (every 6 miles).";
        log.AppendLine($"Encounter: {encounterResult}");

        // 4. Curiosity Phase
        if (RollDice(1, 6) == 1)
        {
            int curiosityRoll = RollDice(1, 20);
            string curiosityDesc = "";
            switch (curiosityRoll)
            {
                case 1:
                    curiosityDesc = "Ruin outcropping: Provides shelter. (1-in-6 chance of encounter).";
                    break;
                case 2:
                    int rats = RollDice(1, 3);
                    _partyState.Rations += rats;
                    curiosityDesc = $"Abandoned camp: Found {rats} rations and a tool.";
                    break;
                case 3:
                    curiosityDesc = "Stone totem: Geometric designs with offerings of effigies.";
                    break;
                case 4:
                    curiosityDesc = "Desiccated corpses: One dead nomad whispers a single word before expiring...";
                    break;
                case 5:
                    curiosityDesc = "Burial cairn: Looted a random weapon and tool.";
                    break;
                case 6:
                    int lodestones = RollDice(1, 10) * 5;
                    curiosityDesc = $"Lodestone cache: Found {lodestones} lodestones under a rock.";
                    break;
                case 7:
                    curiosityDesc = "Nomad in black: Silent figure pointing hand to the nearest pillar.";
                    break;
                case 8:
                    curiosityDesc = "Collapsed tower: Ruins provide shelter.";
                    break;
                case 9:
                    int rawLodestones = RollDice(1, 20);
                    curiosityDesc = $"Lodestone obelisk: Mined {rawLodestones} Raw Lodestones.";
                    break;
                case 10:
                    curiosityDesc = "Tied traveler: Rescued a prisoner from a stone pillar.";
                    break;
                case 11:
                    curiosityDesc = "Unearthed road: Leads to nearest ruins (counts as landmark).";
                    break;
                case 12:
                    int harvestRats = RollDice(1, 3);
                    _partyState.Rations += harvestRats;
                    curiosityDesc = $"Insect swarm: Harvested {harvestRats} rations of bugs.";
                    break;
                case 13:
                    curiosityDesc = "Lonely graves: Care-filled bodies obscured by sand.";
                    break;
                case 14:
                    int nestRats = RollDice(1, 6);
                    _partyState.Rations += nestRats;
                    curiosityDesc = $"Worm nest: Harvested {nestRats} rations of crab/worm meat.";
                    break;
                case 15:
                    curiosityDesc = "Crawl corpse: Massive rotten carcass fed on by many mouths.";
                    break;
                case 16:
                    int tunnelMiles = RollDice(1, 6);
                    curiosityDesc = $"Secret Tunnel: Shelter. Travel {tunnelMiles} miles in random direction.";
                    break;
                case 17:
                    curiosityDesc = "Message slab: Illegible carved stone slab.";
                    break;
                case 18:
                    int caravanRats = RollDice(1, 20);
                    _partyState.Rations += caravanRats;
                    curiosityDesc = $"Lost Caravan: Salvaged {caravanRats} rations/tools.";
                    break;
                case 19:
                    curiosityDesc = "Bereft Swordsman: Rusted blade. Whispers: 'There is no way out.'";
                    break;
                case 20:
                    _partyState.Gold += RollDice(5, 10);
                    curiosityDesc = "Forgotten treasure: Rolled on the Treasure Table (found gold!).";
                    break;
            }
            log.AppendLine($"Curiosity: {curiosityDesc}");
        }

        // 5. Survival Phase (Rations & Starvation)
        int livingCount = 0;
        foreach (var member in _partyState.Members)
        {
            if (member.IsAlive) livingCount++;
        }

        if (livingCount > 0)
        {
            log.AppendLine("• End of day: Ration consumption checked.");
            foreach (var member in _partyState.Members)
            {
                if (!member.IsAlive) continue;
                if (_partyState.Rations >= 1)
                {
                    _partyState.Rations -= 1;
                    if (!wasForcedMarch)
                    {
                        if (member.Exhaustion > 0)
                        {
                            member.Exhaustion = Math.Max(0, member.Exhaustion - 1);
                            log.AppendLine($"  - {member.Name} ate a ration and rested, recovering 1 exhaustion (Exh: {member.Exhaustion}/6).");
                        }
                        else
                        {
                            log.AppendLine($"  - {member.Name} ate a ration.");
                        }
                    }
                    else
                    {
                        log.AppendLine($"  - {member.Name} ate a ration (No recovery due to Forced March today).");
                    }
                }
                else
                {
                    member.Exhaustion = Math.Min(6, member.Exhaustion + 1);
                    log.AppendLine($"  - {member.Name} starved (Exh: {member.Exhaustion}/6)!");
                }
            }
        }

        // 6. Check Exhaustion Deaths
        foreach (var member in _partyState.Members)
        {
            if (member.IsAlive && member.Exhaustion >= 6)
            {
                member.IsAlive = false;
                member.HP = 0;
                log.AppendLine($"☠️ {member.Name} has died of exhaustion!");
            }
        }

        _partyState.DailyLog = log.ToString();
    }

    private void CheckAndResolveHazardCollision(HexCoords coords, List<Hazard> hazards, Dictionary<HexCoords, HexTile> localMap)
    {
        // 1. Find if there is an active hazard on this subhex
        Hazard? activeHazard = null;
        foreach (var h in hazards)
        {
            if (h.IsActive && h.Coords == coords)
            {
                activeHazard = h;
                break;
            }
        }

        if (activeHazard == null) return;

        // Found an active hazard! Let's resolve the collision.
        var log = new System.Text.StringBuilder(_partyState.DailyLog);
        log.AppendLine($"\n⚠️ HAZARD COLLISION: The party encountered a {activeHazard.Name} at subhex {coords}!");
        log.AppendLine($"{activeHazard.Description}");

        var random = new Random();
        bool hazardDestroyed = false;

        switch (activeHazard.Type)
        {
            case 1: // Warband
                // If in Ruins or Settlement, they will not pursue/attack.
                bool inRuinsOrSettlement = false;
                if (localMap.TryGetValue(coords, out var tile))
                {
                    string biome = tile.Biome;
                    if (biome == "Ruins" || biome == "Castle" || biome == "City" || biome == "Campsite" || biome == "Settlement")
                    {
                        inRuinsOrSettlement = true;
                    }
                }

                if (inRuinsOrSettlement)
                {
                    log.AppendLine("🛡️ The Warband patrols outside the ruins/settlement, refusing to enter. The party is safe under cover.");
                }
                else
                {
                    log.AppendLine("⚔️ The Warband ambushes the party! A fierce skirmish erupts.");
                    int numCutthroats = RollDice(5, 6);
                    log.AppendLine($"• The ambush consists of {numCutthroats} Cutthroats led by a Demagogue!");
                    
                    // Slay check: Roll 1d6. On 3-6 (Demagogue slain), on 1-2 (clash with casualties but still slain).
                    int slayRoll = random.Next(1, 7);
                    if (slayRoll >= 3)
                    {
                        log.AppendLine("🎉 Doran challenges the Demagogue to single combat and slays them! The rest of the warband scatters in fear.");
                    }
                    else
                    {
                        log.AppendLine("💥 The Demagogue fights back viciously! Although the party eventually slays the Demagogue and scatters the warband, they suffer heavy damage:");
                        foreach (var member in _partyState.Members)
                        {
                            if (member.IsAlive)
                            {
                                int dmg = RollDice(2, 6);
                                member.HP = Math.Max(0, member.HP - dmg);
                                log.AppendLine($"  - {member.Name} took {dmg} damage (HP: {member.HP}/{member.MaxHP}).");
                                if (member.HP <= 0)
                                {
                                    member.IsAlive = false;
                                    log.AppendLine($"☠️ {member.Name} has been slain in battle!");
                                }
                            }
                        }
                    }
                    hazardDestroyed = true;
                }
                break;

            case 2: // Maelstrom
                // Hiding inside Ruins or sufficiently strong shelter avoids it
                bool maelstromAvoided = _partyState.Sheltered;
                if (localMap.TryGetValue(coords, out var maelstromTile))
                {
                    if (maelstromTile.Biome == "Ruins" || maelstromTile.Biome == "Castle" || maelstromTile.Biome == "City" || maelstromTile.Biome == "Campsite" || maelstromTile.Biome == "Settlement")
                    {
                        maelstromAvoided = true;
                    }
                }

                if (maelstromAvoided)
                {
                    log.AppendLine("🛡️ The party hunkers down in sturdy shelter/ruins, letting the howling winds of the Maelstrom pass harmlessly overhead.");
                }
                else
                {
                    log.AppendLine("🌪️ The party is sucked into the center of the Maelstrom! Each member is buffeted by flying stone and debris:");
                    foreach (var member in _partyState.Members)
                    {
                        if (member.IsAlive)
                        {
                            int dmg = RollDice(3, 20);
                            member.HP = Math.Max(0, member.HP - dmg);
                            log.AppendLine($"  - {member.Name} took {dmg} damage (HP: {member.HP}/{member.MaxHP}).");
                            if (member.HP <= 0)
                            {
                                member.IsAlive = false;
                                log.AppendLine($"☠️ {member.Name} was torn apart by the vortex!");
                            }
                        }
                    }

                    // Fling 1 mile in a random direction
                    int flingDir = random.Next(0, 6);
                    var delta = FlatTopDirections[flingDir];
                    var targetCoords = new HexCoords(coords.Q + delta.Q, coords.R + delta.R);
                    log.AppendLine($"• The party is flung 1 mile in direction index {flingDir + 1} to coordinate {targetCoords}!");

                    // Resolve fling coordinate update (with boundaries and edge transitions if needed)
                    _partyState.DailyLog = log.ToString(); // Store log so far
                    UpdatePartyStatusUI();
                    
                    bool isOffEdge = Math.Abs(targetCoords.Q) > 3 || Math.Abs(targetCoords.R) > 3 || Math.Abs(targetCoords.S) > 3;
                    if (isOffEdge)
                    {
                        if (_renderer.SelectedRegCoords.HasValue)
                        {
                            var parentCoords = _renderer.SelectedRegCoords.Value;
                            HexCoords direction = delta;
                            HexCoords newRegCoords = new HexCoords(parentCoords.Q + direction.Q, parentCoords.R + direction.R);
                            if (_mapData.RegionalTiles.TryGetValue(newRegCoords, out var newRegTile))
                            {
                                HexCoords newLocalCoords = new HexCoords(targetCoords.Q - 6 * direction.Q, targetCoords.R - 6 * direction.R);
                                _partyState.LocalCoords = newLocalCoords;
                                _renderer.PartyLocalCoords = newLocalCoords;
                                _renderer.SelectedLocalCoords = newLocalCoords;
                                log.AppendLine($"• FLUNG OFF THE EDGE into adjacent regional hex {newRegTile.Name} at subhex {newLocalCoords}!");
                                _partyState.DailyLog = log.ToString();
                                EnterLocalScale(newRegCoords);
                                OnLocalSelected(newLocalCoords.Q, newLocalCoords.R);
                                return;
                            }
                            else
                            {
                                log.AppendLine("• The party hits the boundary of the unmapped wastes and bounces back to the edge.");
                                int q = Math.Clamp(targetCoords.Q, -3, 3);
                                int r = Math.Clamp(targetCoords.R, -3, 3);
                                var clampedCoords = new HexCoords(q, r);
                                if (Math.Abs(clampedCoords.S) > 3)
                                {
                                    clampedCoords = coords;
                                }
                                _partyState.LocalCoords = clampedCoords;
                                _renderer.PartyLocalCoords = clampedCoords;
                                _renderer.SelectedLocalCoords = clampedCoords;
                            }
                        }
                    }
                    else
                    {
                        _partyState.LocalCoords = targetCoords;
                        _renderer.PartyLocalCoords = targetCoords;
                        _renderer.SelectedLocalCoords = targetCoords;
                        _partyState.DailyLog = log.ToString();
                        OnLocalSelected(targetCoords.Q, targetCoords.R);
                        return;
                    }
                }
                break;

            case 3: // Crawlherd
                bool inSettlement = false;
                if (localMap.TryGetValue(coords, out var crawlTile))
                {
                    string biome = crawlTile.Biome;
                    if (biome == "Castle" || biome == "City" || biome == "Campsite" || biome == "Settlement")
                    {
                        inSettlement = true;
                    }
                }

                if (inSettlement)
                {
                    log.AppendLine("🛡️ The settlement's solid walls and active guard force keep the Crawlherd from attacking. The party is safe.");
                }
                else
                {
                    log.AppendLine("⚔️ A writhing Crawlherd swarms the party! We must fight our way out.");
                    int numCrawl = RollDice(1, 20);
                    log.AppendLine($"• {numCrawl} Crawl creatures attack!");

                    for (int i = 0; i < numCrawl; i++)
                    {
                        var living = _partyState.Members.FindAll(m => m.IsAlive);
                        if (living.Count == 0) break;
                        var victim = living[random.Next(living.Count)];
                        int dmg = RollDice(1, 3);
                        victim.HP = Math.Max(0, victim.HP - dmg);
                        log.AppendLine($"  - Crawl bites {victim.Name} for {dmg} damage (HP: {victim.HP}/{victim.MaxHP}).");
                        if (victim.HP <= 0)
                        {
                            victim.IsAlive = false;
                            log.AppendLine($"☠️ {victim.Name} was consumed by the Crawl!");
                        }
                    }
                    log.AppendLine("🎉 The Crawlherd is eventually slain and dispersed!");
                    hazardDestroyed = true;
                }
                break;

            case 4: // Collapse
                log.AppendLine("🪨 The cavern ceiling collapses overhead! Heavy stone debris rains down.");
                foreach (var member in _partyState.Members)
                {
                    if (member.IsAlive)
                    {
                        if (member.Exhaustion >= 5)
                        {
                            int dmg = RollDice(4, 10);
                            member.HP = Math.Max(0, member.HP - dmg);
                            log.AppendLine($"  - {member.Name} is already exhausted and cannot run! Suffer {dmg} damage from falling stone (HP: {member.HP}/{member.MaxHP}).");
                            if (member.HP <= 0)
                            {
                                member.IsAlive = false;
                                log.AppendLine($"☠️ {member.Name} was crushed to death!");
                            }
                        }
                        else
                        {
                            member.Exhaustion += 1;
                            log.AppendLine($"  - {member.Name} scrambles to safety, gaining 1 exhaustion level (Exh: {member.Exhaustion}/6).");
                        }
                    }
                }

                if (localMap.TryGetValue(coords, out var collapseTile))
                {
                    string b = collapseTile.Biome;
                    if (b == "Ruins" || b == "Castle" || b == "City" || b == "Campsite" || b == "Settlement")
                    {
                        int structureRoll = random.Next(1, 7);
                        if (structureRoll <= 2)
                        {
                            collapseTile.Biome = "Wastes";
                            collapseTile.Landmark = "None";
                            collapseTile.Name = $"Wastes {coords}";
                            log.AppendLine($"🏚️ The local landmark/structure at {coords} is crushed by the cave-in and reduced to Wastes!");
                        }
                        else
                        {
                            log.AppendLine($"• The structures at {coords} weathered the collapse without structural failure.");
                        }
                    }
                }
                break;

            case 5: // Void Lightning
                bool lightningAvoided = _partyState.Sheltered;
                if (localMap.TryGetValue(coords, out var ltTile))
                {
                    if (ltTile.Biome == "Ruins" || ltTile.Biome == "Castle" || ltTile.Biome == "City" || ltTile.Biome == "Campsite" || ltTile.Biome == "Settlement")
                    {
                        lightningAvoided = true;
                    }
                }

                if (lightningAvoided)
                {
                    log.AppendLine("🛡️ The party shelters beneath ancient stone vaults, grounding the bolts harmlessly.");
                }
                else
                {
                    log.AppendLine("⚡ Jet-black bolts of Void Lightning crackle down from the upper vault!");
                    foreach (var member in _partyState.Members)
                    {
                        if (member.IsAlive)
                        {
                            bool isMetal = (member.Name == "Doran" || member.Name == "Sylas");
                            int threshold = isMetal ? 3 : 1;
                            int strikeRoll = random.Next(1, 7);
                            if (strikeRoll <= threshold)
                            {
                                int dmg = RollDice(10, 6);
                                member.HP = Math.Max(0, member.HP - dmg);
                                log.AppendLine($"💥 Struck! {member.Name} is hit by Void Lightning for {dmg} damage (HP: {member.HP}/{member.MaxHP})!");
                                if (member.HP <= 0)
                                {
                                    member.IsAlive = false;
                                    log.AppendLine($"☠️ {member.Name} was disintegrated into jet-black dust!");
                                }
                            }
                            else
                            {
                                log.AppendLine($"• {member.Name} dodged the electric discharges.");
                            }
                        }
                    }
                }
                break;

            case 6: // Singing Sand
                bool solidGround = false;
                if (localMap.TryGetValue(coords, out var sandTile))
                {
                    string b = sandTile.Biome;
                    if (b == "Pillars" || b == "Ruins" || b == "Castle" || b == "City" || b == "Campsite" || b == "Settlement")
                    {
                        solidGround = true;
                    }
                }

                if (solidGround)
                {
                    log.AppendLine("🛡️ The party stands on high, rocky pillars or stone ruins, safe from the shifting quicksand below.");
                }
                else
                {
                    log.AppendLine("⏳ The ground vibrates with low humming. The dunes dissolve into shifting quicksand!");
                    foreach (var member in _partyState.Members)
                    {
                        if (member.IsAlive)
                        {
                            int saveRoll = random.Next(1, 21);
                            if (saveRoll < 12)
                            {
                                member.HP = 0;
                                member.IsAlive = false;
                                log.AppendLine($"☠️ {member.Name} failed to escape and disappeared forever into the Singing Sand!");
                            }
                            else
                            {
                                log.AppendLine($"• {member.Name} successfully scrambled to solid ground.");
                            }
                        }
                    }
                }
                break;
        }

        if (hazardDestroyed)
        {
            activeHazard.IsActive = false;
            log.AppendLine($"• The threat of {activeHazard.Name} has been cleared from this subhex.");
        }

        _partyState.DailyLog = log.ToString();
        UpdatePartyStatusUI();
    }

    private string ResolveEncounterDescription(int totalRoll, int moodRoll)
    {
        switch (totalRoll)
        {
            case <= 5:
                return "No Encounter: The wastes are silent.";
            case 6:
                return $"Lost Travelers ({RollDice(1, 6)}): Desperate for food/shelter. Helpful if assisted.";
            case 7:
                string mood7 = moodRoll == 1 ? "Cautious, hostile if disturbed" : 
                               (moodRoll <= 4 ? "Curious, peaceful if hailed" : "Friendly, gives directions & warns of danger");
                return $"Nomads ({RollDice(1, 6)}): Mood = {mood7}.";
            case 8:
                return $"Merchants ({RollDice(1, 3)}): Carry supplies on a pulk. Trade Limit: 100g.";
            case 9:
                string mood9 = moodRoll <= 2 ? "Crazed, attacks immediately" : 
                               (moodRoll <= 5 ? "Demands tribute (100 coins or 1 ration/traveler)" : "Curious, offers to join raid");
                return $"Bandits ({RollDice(1, 6)}): Mood = {mood9}.";
            case 10:
                return $"Pilgrims ({RollDice(2, 6)}): Devoted to random faction. Friendly if joined, hostile if contested.";
            case 11:
                return $"Lodestone Prospectors ({RollDice(1, 6)}): Transporting {RollDice(1, 20)} raw lodestones. Hostile if harassed.";
            case 12:
                return $"Caravan ({RollDice(1, 6)} Merchants, {RollDice(2, 6)} Nomads): Trading pulk. Trade Limit: 1000g.";
            case 13:
                string mood13 = moodRoll <= 3 ? "Crazed, attacks to kill" : 
                                (moodRoll <= 5 ? "Demands tribute (1000 coins or ALL rations)" : "Recruit: Demands party duel; survivor joins.");
                return $"Cutthroats ({RollDice(1, 6)}): Mood = {mood13}.";
            case 14:
                return $"Cyclops ({RollDice(1, 6)}): Clustered for warmth, smelling the air for prey.";
            case 15:
                return $"Harpies ({RollDice(1, 3)}): Circling above or hidden under dust.";
            case 16:
                return $"Medusa ({RollDice(1, 3)}): Curled in hidden spots listening for steps.";
            case 17:
                return "Shade: Drifting through the air, vibrating with hunger.";
            case 18:
                return "Griffon: Resting on high point. Remains of latest victim are nearby.";
            default:
                return "No Encounter: The wastes are silent.";
        }
    }

    private int RollDice(int count, int sides)
    {
        var random = new Random();
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            total += random.Next(1, sides + 1);
        }
        return total;
    }

    private static readonly HexCoords[] FlatTopDirections = new[]
    {
        new HexCoords(1, -1),  // 1: North-East
        new HexCoords(1, 0),   // 2: East
        new HexCoords(0, 1),   // 3: South-East
        new HexCoords(-1, 1),  // 4: South-West
        new HexCoords(-1, 0),  // 5: West
        new HexCoords(0, -1)   // 6: North-West
    };
}
