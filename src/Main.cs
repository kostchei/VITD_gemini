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

    public override void _Ready()
    {
        // 1. Generate World Data
        _mapData = Generation.MockGenerator.GenerateWorld();

        // 2. Pre-generate all local maps upfront (so the entire world state is fully created)
        foreach (var regTile in _mapData.RegionalTiles.Values)
        {
            _mapData.GetOrCreateLocalMap(regTile.Coords, (rc) => 
                Generation.MockGenerator.GenerateLocalMap(rc, regTile)
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

        // 6. Set Initial State
        EnterRegionalScale();

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
    }

    private void OnLocalSelected(int q, int r)
    {
        if (!_renderer.SelectedRegCoords.HasValue) return;
        var parentTile = _mapData.RegionalTiles[_renderer.SelectedRegCoords.Value];
        var localMap = _mapData.GetOrCreateLocalMap(_renderer.SelectedRegCoords.Value, (rc) => 
            Generation.MockGenerator.GenerateLocalMap(rc, parentTile)
        );

        var coords = new HexCoords(q, r);
        if (localMap.TryGetValue(coords, out var tile))
        {
            _ui.UpdateUIState(
                MapScale.Local,
                tile.Landmark == "None" ? null : tile.Name,
                $"Q: {q}, R: {r}",
                tile.Biome,
                1
            );
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
        
        // 1. Generate new World Data
        _mapData = Generation.MockGenerator.GenerateWorld();

        // 2. Pre-generate all local maps upfront (so the entire world state is fully created)
        foreach (var regTile in _mapData.RegionalTiles.Values)
        {
            _mapData.GetOrCreateLocalMap(regTile.Coords, (rc) => 
                Generation.MockGenerator.GenerateLocalMap(rc, regTile)
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

    private void EnterLocalScale(HexCoords regCoords)
    {
        var tween = CreateTween();
        tween.TweenProperty(_renderer, "modulate:a", 0.0f, 0.12f);
        tween.TweenCallback(Callable.From(() =>
        {
            _renderer.SetScale(MapScale.Local, regCoords);
            _camera.SetLimits(new Rect2(-700, -500, 1400, 1000));
            ResetCameraForScale(MapScale.Local);

            if (_renderer.SelectedLocalCoords.HasValue)
            {
                OnLocalSelected(_renderer.SelectedLocalCoords.Value.Q, _renderer.SelectedLocalCoords.Value.R);
            }
            else
            {
                _ui.UpdateUIState(MapScale.Local, null, null, null, 1);
            }
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
                // Center on local map origin (0, 0)
                _camera.CenterOn(new Vector2(-60, 0), 0.8f);
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
        string artifactDir = "C:/Users/Admin/.gemini/antigravity-ide/brain/42cd0df8-1c38-46e6-bea4-362ec52336df";
        
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
}
