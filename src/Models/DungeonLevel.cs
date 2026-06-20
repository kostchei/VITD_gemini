namespace VastDark.Models;

public enum DungeonCellType
{
    Wall,
    Floor,
    Room,
    Corridor,
    Door,
    StairsUp,
    StairsDown,
    Chest,
    Encounter
}

public class DungeonLevel
{
    public int LevelNumber { get; } // 1 to 6
    public int Width { get; }
    public int Height { get; }
    public DungeonCellType[,] Cells { get; }

    public DungeonLevel(int levelNumber, int width = 24, int height = 18)
    {
        LevelNumber = levelNumber;
        Width = width;
        Height = height;
        Cells = new DungeonCellType[width, height];
        
        // Initialize all to Wall
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cells[x, y] = DungeonCellType.Wall;
            }
        }
    }
}
