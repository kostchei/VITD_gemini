using Godot;
using System;

namespace VastDark.UI;

public partial class GameUI : CanvasLayer
{
    // Signals
    [Signal] public delegate void BreadcrumbPressedEventHandler(int targetScale); // 0: Regional, 1: Local, 2: Dungeon
    [Signal] public delegate void DungeonLevelChangedEventHandler(int floor);
    [Signal] public delegate void CameraControlPressedEventHandler(string command); // "in", "out", "reset"
    [Signal] public delegate void ActionButtonPressedEventHandler();
    [Signal] public delegate void RegenPressedEventHandler();
    [Signal] public delegate void NextDayPressedEventHandler();

    // Node References
    private Button _btnWorld = null!;
    private Label _arrow1 = null!;
    private Button _btnLocal = null!;
    private Label _arrow2 = null!;
    private Button _btnDungeon = null!;
    private Button _btnNextDay = null!;

    private Label _scaleVal = null!;
    private Label _coordsVal = null!;
    private Label _biomeVal = null!;
    private Label _landmarkVal = null!;
    private Label _detailsVal = null!;
    private Button _btnAction = null!;

    private Control _floorPanel = null!;
    private Button[] _floorButtons = new Button[6];

    public override void _Ready()
    {
        // Bind Top Bar Breadcrumbs
        _btnWorld = GetNode<Button>("TopBar/MarginContainer/HBoxContainer/BtnWorld");
        _arrow1 = GetNode<Label>("TopBar/MarginContainer/HBoxContainer/Arrow1");
        _btnLocal = GetNode<Button>("TopBar/MarginContainer/HBoxContainer/BtnLocal");
        _arrow2 = GetNode<Label>("TopBar/MarginContainer/HBoxContainer/Arrow2");
        _btnDungeon = GetNode<Button>("TopBar/MarginContainer/HBoxContainer/BtnDungeon");

        _btnWorld.Pressed += () => EmitSignal(SignalName.BreadcrumbPressed, 0);
        _btnLocal.Pressed += () => EmitSignal(SignalName.BreadcrumbPressed, 1);
        _btnDungeon.Pressed += () => EmitSignal(SignalName.BreadcrumbPressed, 2);

        // Bind Camera Control Buttons
        GetNode<Button>("TopBar/MarginContainer/HBoxContainer/BtnZoomIn").Pressed += 
            () => EmitSignal(SignalName.CameraControlPressed, "in");
        GetNode<Button>("TopBar/MarginContainer/HBoxContainer/BtnZoomOut").Pressed += 
            () => EmitSignal(SignalName.CameraControlPressed, "out");
        GetNode<Button>("TopBar/MarginContainer/HBoxContainer/BtnResetCam").Pressed += 
            () => EmitSignal(SignalName.CameraControlPressed, "reset");

        // Create Regenerate button dynamically
        var btnRegen = new Button();
        btnRegen.Text = "Regen Map";
        btnRegen.Name = "BtnRegen";
        var container = GetNode<HBoxContainer>("TopBar/MarginContainer/HBoxContainer");
        container.AddChild(btnRegen);
        btnRegen.Pressed += () => EmitSignal(SignalName.RegenPressed);

        // Create Advance Day button dynamically
        _btnNextDay = new Button();
        _btnNextDay.Text = "Next Day";
        _btnNextDay.Name = "BtnNextDay";
        _btnNextDay.Visible = false; // Start hidden (regional scale)
        container.AddChild(_btnNextDay);
        _btnNextDay.Pressed += () => EmitSignal(SignalName.NextDayPressed);

        // Bind Inspector
        _scaleVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/ScaleVal");
        _coordsVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/CoordsVal");
        _biomeVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/BiomeVal");
        _landmarkVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/LandmarkVal");
        _detailsVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/DetailsVal");
        _btnAction = GetNode<Button>("InspectorPanel/MarginContainer/VBoxContainer/BtnAction");
        _btnAction.Pressed += () => EmitSignal(SignalName.ActionButtonPressed);

        // Bind Dungeon Floor Panel
        _floorPanel = GetNode<Control>("FloorPanel");
        for (int i = 0; i < 6; i++)
        {
            int floorIndex = i + 1;
            var btn = GetNode<Button>($"FloorPanel/MarginContainer/VBoxContainer/FloorList/BtnFloor{floorIndex}");
            _floorButtons[i] = btn;
            btn.Pressed += () => EmitSignal(SignalName.DungeonLevelChanged, floorIndex);
        }

        // Set default view state
        UpdateUIState(Common.MapScale.Regional, null, null, null, 1);
    }

    // Updates the HUD text and visibility based on the active scale and selection
    public void UpdateUIState(
        Common.MapScale scale,
        string? selectedName,
        string? coordsText,
        string? biomeText,
        int activeDungeonLevel,
        string? customDetails = null)
    {
        _btnNextDay.Visible = (scale == Common.MapScale.Local);

        // 1. Breadcrumbs Visibility
        switch (scale)
        {
            case Common.MapScale.Regional:
                _btnWorld.Disabled = false;
                _arrow1.Visible = false;
                _btnLocal.Visible = false;
                _arrow2.Visible = false;
                _btnDungeon.Visible = false;
                _floorPanel.Visible = false;
                break;

            case Common.MapScale.Local:
                _btnWorld.Disabled = false;
                _arrow1.Visible = true;
                _btnLocal.Visible = true;
                _btnLocal.Disabled = false;
                _arrow2.Visible = false;
                _btnDungeon.Visible = false;
                _floorPanel.Visible = false;
                break;

            case Common.MapScale.Dungeon:
                _btnWorld.Disabled = false;
                _arrow1.Visible = true;
                _btnLocal.Visible = true;
                _btnLocal.Disabled = false;
                _arrow2.Visible = true;
                _btnDungeon.Visible = true;
                _btnDungeon.Disabled = true; // Already on it
                _floorPanel.Visible = true;
                
                // Highlight the active level button
                for (int i = 0; i < 6; i++)
                {
                    _floorButtons[i].Flat = (i + 1 != activeDungeonLevel);
                }
                break;
        }

        // 2. Inspector Details
        _scaleVal.Text = scale.ToString();
        _coordsVal.Text = coordsText ?? "None Selected";
        _biomeVal.Text = biomeText ?? "-";
        _landmarkVal.Text = selectedName ?? "-";

        if (scale == Common.MapScale.Regional)
        {
            _detailsVal.Text = "Grid: 10x8 Hexes";
            if (selectedName != null && (selectedName.Contains("Dungeon") || selectedName.Contains("Shadowfell") || selectedName.Contains("Neon")))
            {
                _btnAction.Visible = true;
                _btnAction.Text = "Zoom Into Local";
            }
            else if (coordsText != null)
            {
                _btnAction.Visible = true;
                _btnAction.Text = "Zoom Into Local";
            }
            else
            {
                _btnAction.Visible = false;
            }
        }
        else if (scale == Common.MapScale.Local)
        {
            _detailsVal.Text = customDetails ?? "Subhex: 1 Mile Scale";
            if (selectedName != null && selectedName.Contains("Dungeon"))
            {
                _btnAction.Visible = true;
                _btnAction.Text = "Enter Dungeon";
            }
            else
            {
                _btnAction.Visible = false;
            }
        }
        else if (scale == Common.MapScale.Dungeon)
        {
            _detailsVal.Text = $"Floor Layout (Level {activeDungeonLevel})";
            _btnAction.Visible = true;
            _btnAction.Text = "Exit Dungeon";
        }
    }

    public void SetSelectionDetails(string name, string coords, string biome, string details)
    {
        _landmarkVal.Text = name;
        _coordsVal.Text = coords;
        _biomeVal.Text = biome;
        _detailsVal.Text = details;
    }
}
