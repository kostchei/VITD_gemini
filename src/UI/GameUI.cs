using Godot;
using System;
using VastDark.Models;

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
    [Signal] public delegate void ForcedMarchToggledEventHandler(bool pressed);
    [Signal] public delegate void RecenterPressedEventHandler();
    [Signal] public delegate void TradePressedEventHandler();
    [Signal] public delegate void BuyRationPressedEventHandler();
    [Signal] public delegate void BuyHealPressedEventHandler(string characterName);
    [Signal] public delegate void BuyExhaustionPressedEventHandler(string characterName);
    [Signal] public delegate void HireMercenaryPressedEventHandler(string characterName);

    // Node References
    private Button _btnWorld = null!;
    private Label _arrow1 = null!;
    private Button _btnLocal = null!;
    private Label _arrow2 = null!;
    private Button _btnDungeon = null!;
    private Button _btnNextDay = null!;
    private CheckButton _btnForcedMarch = null!;
    private Button _btnRecenter = null!;
    private Button _btnTrade = null!;

    private Label _scaleVal = null!;
    private Label _coordsVal = null!;
    private Label _biomeVal = null!;
    private Label _landmarkVal = null!;
    private Label _detailsVal = null!;
    private Button _btnAction = null!;

    private Label _lblPartyStatus = null!;
    private Label _lblDailyLog = null!;

    public bool IsForcedMarchToggled => _btnForcedMarch.ButtonPressed;

    public void SetForcedMarchToggled(bool toggled)
    {
        _btnForcedMarch.SetPressedNoSignal(toggled);
    }

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

        var inspectorContainer = GetNode<VBoxContainer>("InspectorPanel/MarginContainer/VBoxContainer");

        // Create Advance Day button dynamically
        _btnNextDay = new Button();
        _btnNextDay.Text = "Rest (Next Day)";
        _btnNextDay.Name = "BtnNextDay";
        _btnNextDay.Visible = false; // Start hidden (regional scale)
        inspectorContainer.AddChild(_btnNextDay);
        _btnNextDay.Pressed += () => EmitSignal(SignalName.NextDayPressed);

        // Create Forced March check button dynamically
        _btnForcedMarch = new CheckButton();
        _btnForcedMarch.Text = "Forced March (+6 miles)";
        _btnForcedMarch.Name = "BtnForcedMarch";
        _btnForcedMarch.Visible = false;
        inspectorContainer.AddChild(_btnForcedMarch);
        _btnForcedMarch.Toggled += (pressed) => EmitSignal(SignalName.ForcedMarchToggled, pressed);

        // Create Recenter button dynamically
        _btnRecenter = new Button();
        _btnRecenter.Text = "Recenter on Party";
        _btnRecenter.Name = "BtnRecenter";
        _btnRecenter.Visible = false;
        inspectorContainer.AddChild(_btnRecenter);
        _btnRecenter.Pressed += () => EmitSignal(SignalName.RecenterPressed);

        // Create Trade button dynamically
        _btnTrade = new Button();
        _btnTrade.Text = "🛍️ Open Settlement Shop";
        _btnTrade.Name = "BtnTrade";
        _btnTrade.Visible = false;
        inspectorContainer.AddChild(_btnTrade);
        _btnTrade.Pressed += () => EmitSignal(SignalName.TradePressed);

        // Bind Inspector
        _scaleVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/ScaleVal");
        _coordsVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/CoordsVal");
        _biomeVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/BiomeVal");
        _landmarkVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/LandmarkVal");
        _detailsVal = GetNode<Label>("InspectorPanel/MarginContainer/VBoxContainer/Grid/DetailsVal");
        _btnAction = GetNode<Button>("InspectorPanel/MarginContainer/VBoxContainer/BtnAction");
        _btnAction.Pressed += () => EmitSignal(SignalName.ActionButtonPressed);

        // Add a separator for the party section
        var sep = new HSeparator();
        sep.Name = "PartySeparator";
        inspectorContainer.AddChild(sep);

        // Party Status Label
        _lblPartyStatus = new Label();
        _lblPartyStatus.Name = "PartyStatusVal";
        _lblPartyStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _lblPartyStatus.CustomMinimumSize = new Vector2(250, 0);
        _lblPartyStatus.Text = "Party Vitals...";
        inspectorContainer.AddChild(_lblPartyStatus);

        // Daily Log Label
        _lblDailyLog = new Label();
        _lblDailyLog.Name = "DailyLogVal";
        _lblDailyLog.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _lblDailyLog.CustomMinimumSize = new Vector2(250, 0);
        _lblDailyLog.Text = "Daily Log...";
        inspectorContainer.AddChild(_lblDailyLog);

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
        string? customDetails = null,
        bool isPartyOnSettlement = false)
    {
        if (scale != Common.MapScale.Local)
        {
            HideShop();
        }

        _btnNextDay.Visible = (scale == Common.MapScale.Local);
        _btnForcedMarch.Visible = (scale == Common.MapScale.Local);
        _btnRecenter.Visible = (scale == Common.MapScale.Local || scale == Common.MapScale.Dungeon);
        _lblPartyStatus.Visible = (scale == Common.MapScale.Local || scale == Common.MapScale.Dungeon);
        _lblDailyLog.Visible = (scale == Common.MapScale.Local);
        _btnTrade.Visible = (scale == Common.MapScale.Local && isPartyOnSettlement);

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

    private PanelContainer? _shopPanel = null;
    private Label? _lblShopGold = null;
    private VBoxContainer? _shopItemsContainer = null;

    public void ShowShop(PartyState partyState)
    {
        if (_shopPanel != null)
        {
            _shopPanel.QueueFree();
        }

        // Create Panel Container
        _shopPanel = new PanelContainer();
        _shopPanel.Name = "ShopPanelOverlay";
        
        // Premium Dark Theme styling using StyleBoxFlat
        var style = new StyleBoxFlat();
        style.BgColor = Color.FromHtml("#0f172ae6"); // Slate dark with opacity
        style.BorderColor = Color.FromHtml("#3b82f6"); // Neon blue border
        style.SetBorderWidthAll(2);
        style.CornerRadiusTopLeft = 12;
        style.CornerRadiusTopRight = 12;
        style.CornerRadiusBottomLeft = 12;
        style.CornerRadiusBottomRight = 12;
        style.ContentMarginLeft = 25;
        style.ContentMarginTop = 25;
        style.ContentMarginRight = 25;
        style.ContentMarginBottom = 25;
        _shopPanel.AddThemeStyleboxOverride("panel", style);

        // Center it
        _shopPanel.CustomMinimumSize = new Vector2(450, 400);
        _shopPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center, Control.LayoutPresetMode.Minsize, 0);

        // Add to CanvasLayer
        AddChild(_shopPanel);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 15);
        _shopPanel.AddChild(mainVBox);

        // Title
        var title = new Label();
        title.Text = "🛍️ SETTLEMENT TRADING POST";
        title.ThemeTypeVariation = "HeaderMedium";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        mainVBox.AddChild(title);

        var sep1 = new HSeparator();
        mainVBox.AddChild(sep1);

        // Gold display
        _lblShopGold = new Label();
        _lblShopGold.Text = $"Your Purse: {partyState.Gold} Gold   |   Rations: {partyState.Rations}";
        _lblShopGold.HorizontalAlignment = HorizontalAlignment.Center;
        mainVBox.AddChild(_lblShopGold);

        // Items Container
        _shopItemsContainer = new VBoxContainer();
        _shopItemsContainer.AddThemeConstantOverride("separation", 10);
        mainVBox.AddChild(_shopItemsContainer);

        // Refresh/populate items
        PopulateShopItems(partyState);

        // Close Button
        var btnClose = new Button();
        btnClose.Text = "Exit Shop";
        btnClose.Pressed += HideShop;
        mainVBox.AddChild(btnClose);
    }

    public void PopulateShopItems(PartyState partyState)
    {
        if (_shopItemsContainer == null) return;

        // Clear existing items
        foreach (var child in _shopItemsContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Item 1: Rations (5 Gold)
        var rationHBox = new HBoxContainer();
        var lblRation = new Label();
        lblRation.Text = "Rations (Pack of 1) - Cost: 5 Gold";
        lblRation.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rationHBox.AddChild(lblRation);

        var btnBuyRation = new Button();
        btnBuyRation.Text = "Buy";
        btnBuyRation.Disabled = (partyState.Gold < 5);
        btnBuyRation.Pressed += () => EmitSignal(SignalName.BuyRationPressed);
        rationHBox.AddChild(btnBuyRation);
        _shopItemsContainer.AddChild(rationHBox);

        // Separator
        _shopItemsContainer.AddChild(new HSeparator());

        // Characters healing and recovery
        var charactersTitle = new Label();
        charactersTitle.Text = "🏥 Temples & Inns (Character Services):";
        _shopItemsContainer.AddChild(charactersTitle);

        foreach (var member in partyState.Members)
        {
            var charHBox = new HBoxContainer();
            charHBox.AddThemeConstantOverride("separation", 10);

            var lblCharName = new Label();
            lblCharName.Text = $"{member.Name} ({(member.IsAlive ? $"HP: {member.HP}/{member.MaxHP}, Exh: {member.Exhaustion}" : "☠️ DEAD")})";
            lblCharName.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            charHBox.AddChild(lblCharName);

            if (member.IsAlive)
            {
                // Heal Button (15 Gold)
                var btnHeal = new Button();
                btnHeal.Text = "Heal 10 HP (15g)";
                btnHeal.Disabled = (partyState.Gold < 15 || member.HP >= member.MaxHP);
                btnHeal.Pressed += () => EmitSignal(SignalName.BuyHealPressed, member.Name);
                charHBox.AddChild(btnHeal);

                // Recover Exhaustion Button (20 Gold)
                var btnExh = new Button();
                btnExh.Text = "Rest Exh (20g)";
                btnExh.Disabled = (partyState.Gold < 20 || member.Exhaustion == 0);
                btnExh.Pressed += () => EmitSignal(SignalName.BuyExhaustionPressed, member.Name);
                charHBox.AddChild(btnExh);
            }
            else
            {
                // Revive / Hire Button (50 Gold)
                var btnRevive = new Button();
                btnRevive.Text = "Hire Recruit (50g)";
                btnRevive.Disabled = (partyState.Gold < 50);
                btnRevive.Pressed += () => EmitSignal(SignalName.HireMercenaryPressed, member.Name);
                charHBox.AddChild(btnRevive);
            }

            _shopItemsContainer.AddChild(charHBox);
        }
    }

    public void HideShop()
    {
        if (_shopPanel != null)
        {
            _shopPanel.QueueFree();
            _shopPanel = null;
        }
    }

    public void UpdatePartyUI(string partyStatus, string dailyLog)
    {
        _lblPartyStatus.Text = partyStatus;
        _lblDailyLog.Text = dailyLog;
    }
}
