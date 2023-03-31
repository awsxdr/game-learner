using System.IO;
using System.Linq;

namespace GamePlayer;

public class LevelData
{
    private readonly char[][] _data;

    public char this[int x, int y] => y >= 0 && x >= 0 && y < _data.Length && x < _data[y].Length ? _data[y][x] : '0';

    public int Height => _data.Length;
    public int Width => _data.Any() ? _data[0].Length : 0;

    private LevelData(char[][] data)
    {
        _data = data;
    }

    public static LevelData[] Load(string filePath) =>
        File.ReadAllLines(filePath)
            .Where(x => x.Length > 0)
            .Select((x, i) => (Group: i / 16, Line: x.ToArray()))
            .GroupBy(x => x.Group)
            .Select(g => g.Select(x => x.Line).ToArray())
            .Select(x => new LevelData(x))
            .ToArray();
}