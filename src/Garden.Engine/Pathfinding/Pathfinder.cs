using Garden.Core.World;
using Garden.World.Collections;

namespace Garden.Engine.Pathfinding;

public static class Pathfinder
{
    public static List<(int X, int Y)> FindPath(WorldMap map, int startX, int startY, int endX, int endY)
    {
        var openSet = new SortedSet<(double F, int X, int Y)> { (0, startX, startY) };
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var gScore = new Dictionary<(int, int), double> { [(startX, startY)] = 0 };
        var fScore = new Dictionary<(int, int), double> { [(startX, startY)] = Heuristic(startX, startY, endX, endY) };

        while (openSet.Count > 0)
        {
            var (_, cx, cy) = openSet.Min;
            openSet.Remove(openSet.Min);

            if (cx == endX && cy == endY)
                return ReconstructPath(cameFrom, cx, cy);

            foreach (var neighbor in map.GetNeighbors(cx, cy))
            {
                if (!IsWalkable(neighbor)) continue;

                var tentG = gScore[(cx, cy)] + GetMovementCost(neighbor);
                var key = (neighbor.X, neighbor.Y);

                if (tentG < gScore.GetValueOrDefault(key, double.MaxValue))
                {
                    cameFrom[key] = (cx, cy);
                    gScore[key] = tentG;
                    var f = tentG + Heuristic(neighbor.X, neighbor.Y, endX, endY);
                    fScore[key] = f;
                    openSet.Add((f, neighbor.X, neighbor.Y));
                }
            }
        }

        return [];
    }

    /// <summary>
    /// Finds the nearest tile matching <paramref name="predicate"/> via a single
    /// breadth-first search from the start, stopping at the first match.
    /// Avoids the O(tiles) full-A* re-search per candidate that a naive
    /// "scan every tile, then FindPath to check reachability" approach incurs.
    /// </summary>
    public static List<(int X, int Y)> FindNearestPath(
        WorldMap map, int startX, int startY, Func<World.Entities.WorldTile, bool> predicate, int maxRadius = 60)
    {
        var start = map.GetTile(startX, startY);
        if (predicate(start)) return [(startX, startY)];

        var visited = new HashSet<(int, int)> { (startX, startY) };
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var queue = new Queue<(int X, int Y, int Depth)>();
        queue.Enqueue((startX, startY, 0));

        while (queue.Count > 0)
        {
            var (cx, cy, depth) = queue.Dequeue();
            if (depth >= maxRadius) continue;

            foreach (var neighbor in map.GetNeighbors(cx, cy))
            {
                var key = (neighbor.X, neighbor.Y);
                if (!visited.Add(key)) continue;
                if (!IsWalkable(neighbor)) continue;

                cameFrom[key] = (cx, cy);

                if (predicate(neighbor))
                    return ReconstructPath(cameFrom, neighbor.X, neighbor.Y);

                queue.Enqueue((neighbor.X, neighbor.Y, depth + 1));
            }
        }

        return [];
    }

    private static bool IsWalkable(World.Entities.WorldTile tile)
    {
        return tile.Terrain is not TerrainType.Ocean
            and not TerrainType.Lake
            and not TerrainType.Mountains;
    }

    private static double GetMovementCost(World.Entities.WorldTile tile)
    {
        return tile.Terrain switch
        {
            TerrainType.Plains => 1.0,
            TerrainType.Grassland => 1.1,
            TerrainType.Forest => 1.5,
            TerrainType.Hills => 2.0,
            TerrainType.Swamp => 2.5,
            TerrainType.Coast => 1.2,
            TerrainType.River => 1.8,
            _ => 1.0
        };
    }

    private static double Heuristic(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }

    private static List<(int X, int Y)> ReconstructPath(
        Dictionary<(int, int), (int, int)> cameFrom, int cx, int cy)
    {
        var path = new List<(int X, int Y)> { (cx, cy) };
        var current = (cx, cy);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
