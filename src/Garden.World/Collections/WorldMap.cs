using Garden.World.Entities;

namespace Garden.World.Collections;

public class WorldMap
{
    private WorldTile[,] _tiles = new WorldTile[0, 0];

    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Initialize(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new WorldTile[width, height];
    }

    public WorldTile GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException($"Tile ({x},{y}) out of bounds");
        return _tiles[x, y];
    }

    public void SetTile(int x, int y, WorldTile tile) => _tiles[x, y] = tile;

    public IEnumerable<WorldTile> GetAllTiles()
    {
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < Height; y++)
            yield return _tiles[x, y];
    }

    public IEnumerable<WorldTile> GetNeighbors(int x, int y, int range = 1)
    {
        for (var dx = -range; dx <= range; dx++)
        for (var dy = -range; dy <= range; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            var nx = x + dx;
            var ny = y + dy;
            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                yield return _tiles[nx, ny];
        }
    }
}
