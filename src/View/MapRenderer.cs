using System;
using System.Collections.Generic;
using Godot;
using VastDark.Common;
using VastDark.Models;

namespace VastDark.View;

public partial class MapRenderer : Node2D
{
    // Signals to notify selection changes
    [Signal] public delegate void RegionalSelectedEventHandler(int q, int r);
    [Signal] public delegate void LocalSelectedEventHandler(int q, int r);
    [Signal] public delegate void DungeonSelectedEventHandler(int x, int y);

    [Export] public float RegHexSize = 85.0f;
    [Export] public float LocalHexSize = 42.0f;
    [Export] public float DungeonCellSize = 40.0f;

    // Styling Colors
    private readonly Color BackgroundColor = Color.FromHtml("#e2e8f0");
    private readonly Color LineColor = Color.FromHtml("#475569");
    private readonly Color SelectedOutlineColor = Color.FromHtml("#3b82f6"); // Blue
    private readonly Color HoverOutlineColor = Color.FromHtml("#ef4444"); // Coral Red

    // State Variables
    public MapScale CurrentScale { get; private set; } = MapScale.Regional;
    public MapData? ActiveData { get; private set; }

    public HexCoords? HoveredRegCoords { get; set; }
    public HexCoords? SelectedRegCoords { get; set; }

    public HexCoords? HoveredLocalCoords { get; set; }
    public HexCoords? SelectedLocalCoords { get; set; }

    public Vector2I? HoveredDungeonCell { get; set; }
    public Vector2I? SelectedDungeonCell { get; set; }
    public int ActiveDungeonLevel { get; private set; } = 1;

    private float _time = 0.0f;

    public override void _Ready()
    {
        // Allow receiving inputs
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        // Force redraw to update animated selection ring pulses
        QueueRedraw();
    }

    public void Initialize(MapData data)
    {
        ActiveData = data;
        QueueRedraw();
    }

    public void SetScale(MapScale newScale, HexCoords? parentReg = null, HexCoords? parentLocal = null, int dungeonLevel = 1)
    {
        CurrentScale = newScale;
        ActiveDungeonLevel = dungeonLevel;

        if (newScale == MapScale.Regional)
        {
            // Reset local/dungeon selections
            SelectedLocalCoords = null;
            SelectedDungeonCell = null;
        }
        else if (newScale == MapScale.Local)
        {
            if (parentReg.HasValue) SelectedRegCoords = parentReg;
            SelectedDungeonCell = null;
        }
        else if (newScale == MapScale.Dungeon)
        {
            if (parentReg.HasValue) SelectedRegCoords = parentReg;
            if (parentLocal.HasValue) SelectedLocalCoords = parentLocal;
        }

        QueueRedraw();
    }

    public void SetDungeonLevel(int floor)
    {
        ActiveDungeonLevel = Mathf.Clamp(floor, 1, 6);
        SelectedDungeonCell = null;
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (ActiveData == null) return;

        if (@event is InputEventMouseMotion)
        {
            UpdateHover();
        }
        else if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            HandleClick();
        }
    }

    private void UpdateHover()
    {
        if (ActiveData == null) return;
        Vector2 localMousePos = GetLocalMousePosition();

        switch (CurrentScale)
        {
            case MapScale.Regional:
                var regCoords = HexCoords.FromPixel(localMousePos, RegHexSize, true);
                // Verify if within 10x8 offset bounds
                regCoords.ToOffset(out int rCol, out int rRow);
                if (rCol >= 0 && rCol < MapData.RegWidth && rRow >= 0 && rRow < MapData.RegHeight)
                {
                    if (HoveredRegCoords != regCoords)
                    {
                        HoveredRegCoords = regCoords;
                        QueueRedraw();
                    }
                }
                else if (HoveredRegCoords.HasValue)
                {
                    HoveredRegCoords = null;
                    QueueRedraw();
                }
                break;

            case MapScale.Local:
                Vector2[] outerBoundary = GetLocalOuterBoundary();
                if (Geometry2D.IsPointInPolygon(localMousePos, outerBoundary))
                {
                    var localCoords = HexCoords.FromPixel(localMousePos, LocalHexSize, true);
                    if (Math.Abs(localCoords.Q) <= MapData.LocalRadius &&
                        Math.Abs(localCoords.R) <= MapData.LocalRadius &&
                        Math.Abs(localCoords.S) <= MapData.LocalRadius)
                    {
                        if (HoveredLocalCoords != localCoords)
                        {
                            HoveredLocalCoords = localCoords;
                            QueueRedraw();
                        }
                        break;
                    }
                }

                if (HoveredLocalCoords.HasValue)
                {
                    HoveredLocalCoords = null;
                    QueueRedraw();
                }
                break;

            case MapScale.Dungeon:
                if (SelectedRegCoords.HasValue && SelectedLocalCoords.HasValue)
                {
                    var levels = ActiveData.GetOrCreateDungeon(SelectedRegCoords.Value, SelectedLocalCoords.Value, () => Generation.MockGenerator.GenerateDungeonLevels());
                    var level = levels[ActiveDungeonLevel - 1];

                    // Centered grid drawing coordinate conversion
                    Vector2 gridOrigin = new Vector2(
                        -(level.Width * DungeonCellSize) / 2.0f,
                        -(level.Height * DungeonCellSize) / 2.0f
                    );

                    Vector2 relativePos = localMousePos - gridOrigin;
                    int cx = Mathf.FloorToInt(relativePos.X / DungeonCellSize);
                    int cy = Mathf.FloorToInt(relativePos.Y / DungeonCellSize);

                    if (cx >= 0 && cx < level.Width && cy >= 0 && cy < level.Height)
                    {
                        var cell = new Vector2I(cx, cy);
                        if (HoveredDungeonCell != cell)
                        {
                            HoveredDungeonCell = cell;
                            QueueRedraw();
                        }
                    }
                    else if (HoveredDungeonCell.HasValue)
                    {
                        HoveredDungeonCell = null;
                        QueueRedraw();
                    }
                }
                break;
        }
    }

    private void HandleClick()
    {
        switch (CurrentScale)
        {
            case MapScale.Regional:
                if (HoveredRegCoords.HasValue)
                {
                    if (SelectedRegCoords == HoveredRegCoords.Value)
                    {
                        SelectedRegCoords = null;
                        EmitSignal(SignalName.RegionalSelected, 999, 999);
                    }
                    else
                    {
                        SelectedRegCoords = HoveredRegCoords.Value;
                        EmitSignal(SignalName.RegionalSelected, SelectedRegCoords.Value.Q, SelectedRegCoords.Value.R);
                    }
                }
                else
                {
                    SelectedRegCoords = null;
                    EmitSignal(SignalName.RegionalSelected, 999, 999);
                }
                QueueRedraw();
                break;

            case MapScale.Local:
                if (HoveredLocalCoords.HasValue)
                {
                    if (SelectedLocalCoords == HoveredLocalCoords.Value)
                    {
                        SelectedLocalCoords = null;
                        EmitSignal(SignalName.LocalSelected, 999, 999);
                    }
                    else
                    {
                        SelectedLocalCoords = HoveredLocalCoords.Value;
                        EmitSignal(SignalName.LocalSelected, SelectedLocalCoords.Value.Q, SelectedLocalCoords.Value.R);
                    }
                }
                else
                {
                    SelectedLocalCoords = null;
                    EmitSignal(SignalName.LocalSelected, 999, 999);
                }
                QueueRedraw();
                break;

            case MapScale.Dungeon:
                if (HoveredDungeonCell.HasValue)
                {
                    if (SelectedDungeonCell == HoveredDungeonCell.Value)
                    {
                        SelectedDungeonCell = null;
                        EmitSignal(SignalName.DungeonSelected, -1, -1);
                    }
                    else
                    {
                        SelectedDungeonCell = HoveredDungeonCell.Value;
                        EmitSignal(SignalName.DungeonSelected, SelectedDungeonCell.Value.X, SelectedDungeonCell.Value.Y);
                    }
                }
                else
                {
                    SelectedDungeonCell = null;
                    EmitSignal(SignalName.DungeonSelected, -1, -1);
                }
                QueueRedraw();
                break;
        }
    }

    public override void _Draw()
    {
        if (ActiveData == null) return;

        // Clear background
        DrawRect(new Rect2(-5000, -5000, 10000, 10000), BackgroundColor);

        switch (CurrentScale)
        {
            case MapScale.Regional:
                DrawRegionalMap();
                break;

            case MapScale.Local:
                DrawLocalMap();
                break;

            case MapScale.Dungeon:
                DrawDungeonMap();
                break;
        }
    }

    private void DrawRegionalMap()
    {
        if (ActiveData == null) return;

        // Draw Tiles
        foreach (var pair in ActiveData.RegionalTiles)
        {
            var coords = pair.Key;
            var tile = pair.Value;

            Vector2 center = coords.ToPixel(RegHexSize, true);
            Vector2[] vertices = GetHexVertices(center, RegHexSize, true);

            // Draw Hex Polygon (Background + Dither)
            FillAndDitherHex(vertices, tile.Biome, RegHexSize);

            // Draw Outline
            DrawHexOutline(center, RegHexSize, LineColor, 1.0f, true);

            // Draw Landmark symbol or name
            if (tile.Landmark != "None")
            {
                DrawLandmarkSymbol(center, tile.Landmark, RegHexSize * 0.4f);
                DrawTextCentered(tile.Name, center + new Vector2(0, RegHexSize * 0.55f), 12, Color.FromHtml("#0f172a"));
            }
            else
            {
                // Simple coord label
                DrawTextCentered($"{coords.Q},{coords.R}", center, 10, Color.FromHtml("#64748b"));
            }
        }

        // Draw Hover Outline
        if (HoveredRegCoords.HasValue)
        {
            Vector2 center = HoveredRegCoords.Value.ToPixel(RegHexSize, true);
            float pulse = 1.0f + 0.03f * Mathf.Cos(_time * 8.0f);
            DrawHexOutline(center, RegHexSize * pulse, HoverOutlineColor, 2.0f, true);
        }

        // Draw Selection Outline
        if (SelectedRegCoords.HasValue)
        {
            Vector2 center = SelectedRegCoords.Value.ToPixel(RegHexSize, true);
            float pulse = 1.0f + 0.05f * Mathf.Sin(_time * 5.0f);
            DrawHexOutline(center, RegHexSize * pulse, SelectedOutlineColor, 3.5f, true);
        }
    }

    private void DrawLocalMap()
    {
        if (ActiveData == null || !SelectedRegCoords.HasValue) return;

        var parentTile = ActiveData.RegionalTiles[SelectedRegCoords.Value];
        var localMap = ActiveData.GetOrCreateLocalMap(SelectedRegCoords.Value, (rc) => 
            Generation.MockGenerator.GenerateLocalMap(rc, parentTile)
        );

        // Draw Tiles
        foreach (var pair in localMap)
        {
            var coords = pair.Key;
            var tile = pair.Value;

            Vector2[] clippedVertices = GetClippedLocalSubhexVertices(coords);
            if (clippedVertices.Length == 0) continue;

            // Draw Hex Polygon (Background + Dither)
            FillAndDitherHex(clippedVertices, tile.Biome, LocalHexSize);

            // Draw Outline
            Vector2[] loop = new Vector2[clippedVertices.Length + 1];
            Array.Copy(clippedVertices, loop, clippedVertices.Length);
            loop[clippedVertices.Length] = clippedVertices[0];
            DrawPolyline(loop, LineColor * 1.5f, 1.0f, true);

            // Draw symbol or abbreviation at pointy-topped center
            Vector2 center = coords.ToPixel(LocalHexSize, true);
            if (tile.Landmark != "None")
            {
                DrawLandmarkSymbol(center, tile.Landmark, LocalHexSize * 0.45f);
            }
            else if (tile.Biome != "Wastes")
            {
                // Soft abbreviation
                string abbrev = tile.Biome.Length > 3 ? tile.Biome.Substring(0, 3) : tile.Biome;
                DrawTextCentered(abbrev.ToUpper(), center, 9, Color.FromHtml("#475569"));
            }
        }

        // Draw Hazards
        var hazards = ActiveData.GetOrCreateLocalHazards(SelectedRegCoords.Value, (rc) => 
            Generation.MockGenerator.GenerateLocalHazards(rc, localMap)
        );
        foreach (var hazard in hazards)
        {
            if (!hazard.IsActive) continue;
            Vector2 center = hazard.Coords.ToPixel(LocalHexSize, true);
            DrawHazardSymbol(center, hazard.Type, LocalHexSize * 0.45f);
        }

        // Draw Hover Outline
        if (HoveredLocalCoords.HasValue)
        {
            float pulse = 1.0f + 0.03f * Mathf.Cos(_time * 8.0f);
            Vector2[] clippedVertices = GetClippedLocalSubhexVertices(HoveredLocalCoords.Value, pulse);
            if (clippedVertices.Length > 0)
            {
                Vector2[] loop = new Vector2[clippedVertices.Length + 1];
                Array.Copy(clippedVertices, loop, clippedVertices.Length);
                loop[clippedVertices.Length] = clippedVertices[0];
                DrawPolyline(loop, HoverOutlineColor, 2.0f, true);
            }
        }

        // Draw Selection Outline
        if (SelectedLocalCoords.HasValue)
        {
            float pulse = 1.0f + 0.05f * Mathf.Sin(_time * 5.0f);
            Vector2[] clippedVertices = GetClippedLocalSubhexVertices(SelectedLocalCoords.Value, pulse);
            if (clippedVertices.Length > 0)
            {
                Vector2[] loop = new Vector2[clippedVertices.Length + 1];
                Array.Copy(clippedVertices, loop, clippedVertices.Length);
                loop[clippedVertices.Length] = clippedVertices[0];
                DrawPolyline(loop, SelectedOutlineColor, 3.0f, true);
            }
        }

        // Draw regional hex outer boundary on top
        Vector2[] outerBoundary = GetLocalOuterBoundary();
        Vector2[] boundaryLoop = new Vector2[7];
        Array.Copy(outerBoundary, boundaryLoop, 6);
        boundaryLoop[6] = outerBoundary[0];
        DrawPolyline(boundaryLoop, Color.FromHtml("#3b82f6"), 4.0f, true); // Sturdy bright blue boundary
    }

    private void DrawDungeonMap()
    {
        if (ActiveData == null || !SelectedRegCoords.HasValue || !SelectedLocalCoords.HasValue) return;

        var levels = ActiveData.GetOrCreateDungeon(SelectedRegCoords.Value, SelectedLocalCoords.Value, () => 
            Generation.MockGenerator.GenerateDungeonLevels()
        );
        var level = levels[ActiveDungeonLevel - 1];

        // Draw centered grid
        Vector2 gridOrigin = new Vector2(
            -(level.Width * DungeonCellSize) / 2.0f,
            -(level.Height * DungeonCellSize) / 2.0f
        );

        // Draw cells
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                var cellType = level.Cells[x, y];
                Vector2 cellPos = gridOrigin + new Vector2(x * DungeonCellSize, y * DungeonCellSize);
                Rect2 cellRect = new Rect2(cellPos, new Vector2(DungeonCellSize, DungeonCellSize));

                // Determine Cell Color
                Color fillCol = cellType switch
                {
                    DungeonCellType.Wall => Color.FromHtml("#1a1c23"),
                    DungeonCellType.Floor => Color.FromHtml("#2c303c"),
                    DungeonCellType.Room => Color.FromHtml("#383e4e"),
                    DungeonCellType.Corridor => Color.FromHtml("#262933"),
                    DungeonCellType.Door => Color.FromHtml("#4b5320"),
                    DungeonCellType.StairsUp => Color.FromHtml("#0284c7"), // Blue
                    DungeonCellType.StairsDown => Color.FromHtml("#0369a1"), // Dark Blue
                    DungeonCellType.Chest => Color.FromHtml("#ca8a04"), // Gold
                    DungeonCellType.Encounter => Color.FromHtml("#b91c1c"), // Red
                    _ => Color.FromHtml("#000000")
                };

                // Draw Rect
                DrawRect(cellRect, fillCol);
                
                // Draw inner borders for rooms/floors
                if (cellType != DungeonCellType.Wall)
                {
                    DrawRect(cellRect, LineColor * 0.8f, false, 1.0f);
                }

                // Draw Vector Icons for features
                Vector2 cellCenter = cellPos + new Vector2(DungeonCellSize / 2.0f, DungeonCellSize / 2.0f);
                DrawDungeonSymbol(cellCenter, cellType);
            }
        }

        // Draw Hover cell outline
        if (HoveredDungeonCell.HasValue)
        {
            Vector2 cellPos = gridOrigin + new Vector2(HoveredDungeonCell.Value.X * DungeonCellSize, HoveredDungeonCell.Value.Y * DungeonCellSize);
            Rect2 cellRect = new Rect2(cellPos, new Vector2(DungeonCellSize, DungeonCellSize));
            float pulse = 1.0f + 0.02f * Mathf.Cos(_time * 8.0f);
            Vector2 size = cellRect.Size * pulse;
            Vector2 offset = (cellRect.Size - size) / 2.0f;
            DrawRect(new Rect2(cellRect.Position + offset, size), HoverOutlineColor, false, 2.0f);
        }

        // Draw Selected cell outline
        if (SelectedDungeonCell.HasValue)
        {
            Vector2 cellPos = gridOrigin + new Vector2(SelectedDungeonCell.Value.X * DungeonCellSize, SelectedDungeonCell.Value.Y * DungeonCellSize);
            Rect2 cellRect = new Rect2(cellPos, new Vector2(DungeonCellSize, DungeonCellSize));
            float pulse = 1.0f + 0.04f * Mathf.Sin(_time * 5.0f);
            Vector2 size = cellRect.Size * pulse;
            Vector2 offset = (cellRect.Size - size) / 2.0f;
            DrawRect(new Rect2(cellRect.Position + offset, size), SelectedOutlineColor, false, 3.0f);
        }
    }

    #region Geometry and Custom Vector Drawer Helpers
    public Vector2[] GetLocalOuterBoundary()
    {
        Vector2[] boundary = new Vector2[6];
        float outerRadius = 6.0f * LocalHexSize; // Both scales are flat-topped, ratio is 6:1 flat-to-flat
        for (int i = 0; i < 6; i++)
        {
            float angleRad = Mathf.DegToRad(60.0f * i);
            boundary[i] = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * outerRadius;
        }
        return boundary;
    }

    private Vector2[] GetClippedLocalSubhexVertices(HexCoords coords, float scale = 1.0f)
    {
        Vector2 center = coords.ToPixel(LocalHexSize, true);
        Vector2[] subhexVertices = GetHexVertices(center, LocalHexSize * scale, true);
        Vector2[] outerBoundary = GetLocalOuterBoundary();
        var intersections = Geometry2D.IntersectPolygons(subhexVertices, outerBoundary);
        if (intersections.Count > 0)
        {
            return intersections[0];
        }
        return Array.Empty<Vector2>();
    }

    private Vector2[] GetHexVertices(Vector2 center, float size, bool flatTopped)
    {
        Vector2[] vertices = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60.0f * i + (flatTopped ? 0.0f : 30.0f);
            float angleRad = Mathf.DegToRad(angleDeg);
            vertices[i] = center + new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * size;
        }
        return vertices;
    }

    private void DrawHexOutline(Vector2 center, float size, Color color, float width, bool flatTopped)
    {
        Vector2[] vertices = GetHexVertices(center, size, flatTopped);
        Vector2[] loop = new Vector2[7];
        Array.Copy(vertices, loop, 6);
        loop[6] = vertices[0];
        DrawPolyline(loop, color, width, true);
    }

    private void DrawTextCentered(string text, Vector2 pos, int fontSize, Color color)
    {
        Font font = ThemeDB.FallbackFont;
        // Correct drawing centering offset
        Vector2 stringSize = font.GetStringSize(text, HorizontalAlignment.Left, -1.0f, fontSize);
        Vector2 drawPos = pos + new Vector2(-stringSize.X / 2.0f, stringSize.Y / 3.0f);
        DrawString(font, drawPos, text, HorizontalAlignment.Center, -1, fontSize, color);
    }

    private void DrawRuinsIcon(Vector2 center, float size, Color color, float width = 3.0f)
    {
        float w = size;
        Vector2[] points = new Vector2[]
        {
            center + new Vector2(-0.5f * w, 0.4f * w),
            center + new Vector2(-0.5f * w, 0.0f * w),
            center + new Vector2(-0.25f * w, 0.0f * w),
            center + new Vector2(-0.25f * w, 0.2f * w),
            center + new Vector2(-0.12f * w, 0.2f * w),
            center + new Vector2(-0.12f * w, -0.4f * w),
            center + new Vector2(0.12f * w, -0.4f * w),
            center + new Vector2(0.12f * w, 0.1f * w),
            center + new Vector2(0.18f * w, 0.1f * w),
            center + new Vector2(0.18f * w, -0.2f * w),
            center + new Vector2(0.38f * w, -0.2f * w),
            center + new Vector2(0.38f * w, 0.2f * w),
            center + new Vector2(0.48f * w, 0.2f * w),
            center + new Vector2(0.48f * w, 0.0f * w),
            center + new Vector2(0.58f * w, 0.0f * w),
            center + new Vector2(0.58f * w, 0.4f * w)
        };
        DrawPolyline(points, color, width, true);
    }

    private void DrawSettlementIcon(Vector2 center, float size, Color color, float width = 3.0f)
    {
        float w = size;
        // First draw the ruins skyline
        DrawRuinsIcon(center, size, color, width);

        // Draw flagpole on the left tower (starts at top-middle of the left tower, x = -0.375)
        float poleX = -0.375f * w;
        Vector2 poleStart = center + new Vector2(poleX, 0.0f * w);
        Vector2 poleEnd = center + new Vector2(poleX, -0.45f * w);
        DrawLine(poleStart, poleEnd, color, width, true);

        // Draw waving flag pointing left
        Vector2[] flagPoints = new Vector2[]
        {
            poleEnd,
            center + new Vector2(poleX - 0.15f * w, -0.47f * w),
            center + new Vector2(poleX - 0.32f * w, -0.40f * w),
            center + new Vector2(poleX - 0.22f * w, -0.28f * w),
            center + new Vector2(poleX - 0.12f * w, -0.31f * w),
            center + new Vector2(poleX, -0.22f * w)
        };
        DrawColoredPolygon(flagPoints, color);
    }

    private void DrawLandmarkSymbol(Vector2 center, string type, float size)
    {
        if (type == "Dungeon" || type.Contains("Dungeon"))
        {
            // Draw glowing diamond portal
            Vector2[] points = new[]
            {
                center + new Vector2(0, -size),
                center + new Vector2(size * 0.8f, 0),
                center + new Vector2(0, size),
                center + new Vector2(-size * 0.8f, 0)
            };
            DrawColoredPolygon(points, Color.FromHtml("#ec4899")); // Pink portal fill
            DrawPolyline(new[] { points[0], points[1], points[2], points[3], points[0] }, Color.FromHtml("#ff007f"), 2.0f, true);
            
            // Core
            DrawCircle(center, size * 0.25f, Color.FromHtml("#ffffff"));
        }
        else if (type == "Castle" || type == "City" || type == "Campsite" || type == "Settlement" || type == "Settlements")
        {
            DrawSettlementIcon(center, size, Color.FromHtml("#000000"), 3.0f);
        }
        else if (type == "Ruins")
        {
            DrawRuinsIcon(center, size, Color.FromHtml("#000000"), 3.0f);
        }
    }

    private void DrawDungeonSymbol(Vector2 center, DungeonCellType type)
    {
        float size = DungeonCellSize * 0.3f;
        switch (type)
        {
            case DungeonCellType.Door:
                // Draw a simple door line inside
                DrawLine(center + new Vector2(-size, 0), center + new Vector2(size, 0), Color.FromHtml("#facc15"), 3.0f);
                break;
            case DungeonCellType.StairsUp:
                // Stair icon going up
                for (int i = 0; i < 3; i++)
                {
                    float y = center.Y + size - i * (size * 0.8f);
                    float xStart = center.X - size + i * (size * 0.8f);
                    DrawRect(new Rect2(xStart, y - 2, size * 0.8f, 4), Color.FromHtml("#38bdf8"));
                }
                break;
            case DungeonCellType.StairsDown:
                // Stair icon going down
                for (int i = 0; i < 3; i++)
                {
                    float y = center.Y - size + i * (size * 0.8f);
                    float xStart = center.X - size + i * (size * 0.8f);
                    DrawRect(new Rect2(xStart, y - 2, size * 0.8f, 4), Color.FromHtml("#0284c7"));
                }
                break;
            case DungeonCellType.Chest:
                // Draw small box
                DrawRect(new Rect2(center.X - size * 0.9f, center.Y - size * 0.6f, size * 1.8f, size * 1.2f), Color.FromHtml("#eab308"));
                DrawCircle(center, 2.0f, Color.FromHtml("#000000")); // Keyhole
                break;
            case DungeonCellType.Encounter:
                // Draw warning diamond
                Vector2[] pts = new[] { center + new Vector2(0, -size), center + new Vector2(size, 0), center + new Vector2(0, size), center + new Vector2(-size, 0) };
                DrawColoredPolygon(pts, Color.FromHtml("#ef4444"));
                break;
        }
    }

    private Color GetBiomeColor(string biome)
    {
        return biome switch
        {
            "Wastes" => Color.FromHtml("#252830"),
            "Ruins" => Color.FromHtml("#4a4d53"),
            "Pillars" => Color.FromHtml("#111215"),
            "Ocean" => Color.FromHtml("#161a2b"),
            "Plains" => Color.FromHtml("#132d20"),
            "Forest" => Color.FromHtml("#0a2218"),
            "Mountains" => Color.FromHtml("#263238"),
            "Swamp" => Color.FromHtml("#2d1830"),
            "Desert" => Color.FromHtml("#3e2723"),
            _ => Color.FromHtml("#1f2421")
        };
    }

    private Color GetLocalBiomeColor(string biome)
    {
        return biome switch
        {
            "Wastes" => Color.FromHtml("#252830"),
            "Ruins" => Color.FromHtml("#4a4d53"),
            "Settlements" => Color.FromHtml("#314f3c"),
            "Pillars" => Color.FromHtml("#111215"),
            "Deep Water" => Color.FromHtml("#0d2e5c"),
            "Peak" => Color.FromHtml("#a5f3fc"),
            "Ridge" => Color.FromHtml("#475569"),
            "Rocky Hills" => Color.FromHtml("#403d39"),
            "Ancient Tree" => Color.FromHtml("#10b981"),
            "Dense Woods" => Color.FromHtml("#065f46"),
            "Clearic Forest" => Color.FromHtml("#047857"),
            "Brog" => Color.FromHtml("#581c87"),
            "Miry Marsh" => Color.FromHtml("#3f6212"),
            "Sand Dunes" => Color.FromHtml("#b45309"),
            "Rocky Flats" => Color.FromHtml("#78350f"),
            "Meadow" => Color.FromHtml("#15803d"),
            "Plains Grass" => Color.FromHtml("#16a34a"),
            "Campsite" => Color.FromHtml("#d97706"),
            "Dungeon Gate" => Color.FromHtml("#db2777"),
            "Castle Gate" => Color.FromHtml("#475569"),
            "City Gate" => Color.FromHtml("#7c3aed"),
            _ => Color.FromHtml("#2e3f28")
        };
    }

    private void DrawHazardSymbol(Vector2 center, int type, float size)
    {
        // Draw a premium glowing circular backing for the hazard to make it stand out
        Color backColor = Color.FromHtml("#0f172a"); // Dark slate background
        Color glowColor = type switch
        {
            1 => Color.FromHtml("#ef4444"), // Red for Warband
            2 => Color.FromHtml("#a855f7"), // Purple/violet for Maelstrom
            3 => Color.FromHtml("#22c55e"), // Green for Crawlherd
            4 => Color.FromHtml("#f97316"), // Orange for Collapse
            5 => Color.FromHtml("#3b82f6"), // Blue for Void Lightning
            6 => Color.FromHtml("#eab308"), // Yellow/amber for Singing Sand
            _ => Color.FromHtml("#64748b")
        };

        // Draw backing circle
        DrawCircle(center, size, backColor);
        // Draw outer glow outline
        DrawCircle(center, size, glowColor, false, 2.0f);

        // Draw the hazard type icon
        float innerSize = size * 0.6f;
        switch (type)
        {
            case 1: // Warband: Draw crossed axes / lines
                DrawLine(center + new Vector2(-innerSize, -innerSize), center + new Vector2(innerSize, innerSize), glowColor, 2.0f, true);
                DrawLine(center + new Vector2(innerSize, -innerSize), center + new Vector2(-innerSize, innerSize), glowColor, 2.0f, true);
                // Draw a small cross bar
                DrawLine(center + new Vector2(-innerSize * 0.5f, 0), center + new Vector2(innerSize * 0.5f, 0), glowColor, 2.0f, true);
                break;

            case 2: // Maelstrom: Draw a spiral/swirl
                int pointsCount = 12;
                Vector2[] spiral = new Vector2[pointsCount];
                for (int i = 0; i < pointsCount; i++)
                {
                    float angle = i * 0.8f + _time * 2.0f;
                    float radius = innerSize * ((float)i / pointsCount);
                    spiral[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                DrawPolyline(spiral, glowColor, 2.0f, true);
                break;

            case 3: // Crawlherd: Draw a cluster of dots
                DrawCircle(center + new Vector2(-innerSize * 0.4f, -innerSize * 0.2f), 2.0f, glowColor);
                DrawCircle(center + new Vector2(innerSize * 0.4f, -innerSize * 0.3f), 2.0f, glowColor);
                DrawCircle(center + new Vector2(-innerSize * 0.2f, innerSize * 0.4f), 2.0f, glowColor);
                DrawCircle(center + new Vector2(innerSize * 0.1f, innerSize * 0.1f), 2.5f, glowColor);
                DrawCircle(center + new Vector2(0, -innerSize * 0.5f), 1.5f, glowColor);
                break;

            case 4: // Collapse: Draw a falling debris/triangle
                Vector2[] rubble = new Vector2[]
                {
                    center + new Vector2(0, -innerSize),
                    center + new Vector2(innerSize * 0.8f, innerSize * 0.6f),
                    center + new Vector2(-innerSize * 0.8f, innerSize * 0.6f),
                    center + new Vector2(0, -innerSize)
                };
                DrawPolyline(rubble, glowColor, 2.0f, true);
                DrawLine(center + new Vector2(-innerSize * 0.4f, innerSize * 0.2f), center + new Vector2(innerSize * 0.4f, innerSize * 0.2f), glowColor, 1.5f, true);
                break;

            case 5: // Void Lightning: Draw a lightning bolt
                Vector2[] bolt = new Vector2[]
                {
                    center + new Vector2(innerSize * 0.3f, -innerSize),
                    center + new Vector2(-innerSize * 0.3f, 0),
                    center + new Vector2(innerSize * 0.2f, 0),
                    center + new Vector2(-innerSize * 0.2f, innerSize)
                };
                DrawPolyline(bolt, glowColor, 2.0f, true);
                break;

            case 6: // Singing Sand: Draw wavy lines
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    float y = center.Y + yOffset * innerSize * 0.5f;
                    Vector2[] wave = new Vector2[7];
                    for (int i = 0; i < 7; i++)
                    {
                        float x = center.X - innerSize + (innerSize * 2.0f * i / 6.0f);
                        float sine = Mathf.Sin(i * 1.5f + _time * 4.0f) * 3.0f;
                        wave[i] = new Vector2(x, y + sine);
                    }
                    DrawPolyline(wave, glowColor, 1.5f, true);
                }
                break;
        }

        // Draw the hazard face value (1-6) as a small label in the center
        DrawTextCentered(type.ToString(), center, 10, Color.FromHtml("#ffffff"));
    }

    private void FillAndDitherHex(Vector2[] vertices, string biome, float size)
    {
        // 1. Get solid backing color
        Color backingColor = biome switch
        {
            "Wastes" => Color.FromHtml("#fafafa"),
            "Ruins" => Color.FromHtml("#f1f5f9"),
            "Settlements" or "Campsite" or "Castle" or "City" or "Settlement" or "Castle Gate" or "City Gate" => Color.FromHtml("#fef9c3"),
            "Pillars" => Color.FromHtml("#334155"),
            "Dungeon Gate" or "Dungeon" => Color.FromHtml("#334155"),
            "Ocean" or "Deep Water" => Color.FromHtml("#bae6fd"),
            "Plains" or "Plains Grass" or "Meadow" => Color.FromHtml("#dcfce7"),
            "Forest" or "Dense Woods" or "Clearic Forest" or "Ancient Tree" => Color.FromHtml("#bbf7d0"),
            "Mountains" or "Peak" or "Ridge" or "Rocky Hills" => Color.FromHtml("#cbd5e1"),
            "Swamp" or "Brog" or "Miry Marsh" => Color.FromHtml("#f3e8ff"),
            "Desert" or "Sand Dunes" or "Rocky Flats" => Color.FromHtml("#fde68a"),
            _ => Color.FromHtml("#fafafa")
        };

        // Draw solid background polygon
        DrawColoredPolygon(vertices, backingColor);

        // 2. Draw stipple/dither overlay if applicable
        int step = size > 50.0f ? 8 : 4;
        float dotRadius = size > 50.0f ? 1.2f : 0.8f;

        bool isSettlement = biome == "Settlements" || biome == "Campsite" || biome == "Castle" || biome == "City" || biome == "Settlement" || biome == "Castle Gate" || biome == "City Gate";

        if (biome == "Ruins")
        {
            DrawDitheredPolygon(vertices, Color.FromHtml("#94a3b8"), dotRadius, step);
        }
        else if (isSettlement)
        {
            DrawDitheredPolygon(vertices, Color.FromHtml("#ca8a04"), dotRadius, step);
        }
        else if (biome == "Pillars" || biome == "Dungeon Gate" || biome == "Dungeon")
        {
            DrawDitheredPolygon(vertices, Color.FromHtml("#0f172a"), dotRadius, step);
        }
    }

    private void DrawDitheredPolygon(Vector2[] vertices, Color dotColor, float dotRadius, int step)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var v in vertices)
        {
            if (v.X < minX) minX = v.X;
            if (v.X > maxX) maxX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.Y > maxY) maxY = v.Y;
        }

        for (float y = minY; y <= maxY; y += step)
        {
            // Stagger alternating rows for a checkerboard/hexagonal pattern
            float offsetX = ((int)Mathf.Round(y / step) % 2 == 0) ? 0 : step / 2.0f;
            for (float x = minX - offsetX; x <= maxX; x += step)
            {
                Vector2 point = new Vector2(x + offsetX, y);
                if (Geometry2D.IsPointInPolygon(point, vertices))
                {
                    DrawCircle(point, dotRadius, dotColor);
                }
            }
        }
    }
    #endregion
}
