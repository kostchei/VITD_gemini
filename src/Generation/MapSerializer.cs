using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using VastDark.Common;
using VastDark.Models;

namespace VastDark.Generation;

public static class MapSerializer
{
    private class SerializedWorld
    {
        public List<SerializedRegionalTile> RegionalTiles { get; set; } = new();
    }

    private class SerializedRegionalTile
    {
        public int Q { get; set; }
        public int R { get; set; }
        public string Biome { get; set; } = "";
        public string Landmark { get; set; } = "";
        public string Name { get; set; } = "";
        public List<SerializedLocalTile> LocalTiles { get; set; } = new();
    }

    private class SerializedLocalTile
    {
        public int Q { get; set; }
        public int R { get; set; }
        public string Biome { get; set; } = "";
        public string Landmark { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public static void SaveToFile(MapData mapData, string path)
    {
        try
        {
            var worldDto = new SerializedWorld();

            foreach (var regPair in mapData.RegionalTiles)
            {
                var regCoords = regPair.Key;
                var regTile = regPair.Value;

                var regDto = new SerializedRegionalTile
                {
                    Q = regCoords.Q,
                    R = regCoords.R,
                    Biome = regTile.Biome,
                    Landmark = regTile.Landmark,
                    Name = regTile.Name
                };

                // Get or generate local map for this regional hex
                var localMap = mapData.GetOrCreateLocalMap(regCoords, (rc) => 
                    MockGenerator.GenerateLocalMap(rc, regTile)
                );

                foreach (var localPair in localMap)
                {
                    var localCoords = localPair.Key;
                    var localTile = localPair.Value;

                    regDto.LocalTiles.Add(new SerializedLocalTile
                    {
                        Q = localCoords.Q,
                        R = localCoords.R,
                        Biome = localTile.Biome,
                        Landmark = localTile.Landmark,
                        Name = localTile.Name
                    });
                }

                worldDto.RegionalTiles.Add(regDto);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(worldDto, options);
            File.WriteAllText(path, json);
            GD.Print($"[SaveSystem] Successfully saved map data to: {path}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveSystem] Failed to save map data: {ex.Message}");
        }
    }
}
