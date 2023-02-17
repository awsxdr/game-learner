namespace GamePlayer;

using System.Collections.Generic;
using System.Linq;

public static class ExtensionMethods
{
    public static Dictionary<char, Tile> ToTileDictionary(this IEnumerable<Tile> tiles) =>
        tiles.ToDictionary(t => t.Name, t => t);

    public static IEnumerable<(T First, T Second)> GetCombinations<T>(this IEnumerable<T> items)
    {
        items = items.ToArray();

        for (var i = 0; i < items.Count() - 1; ++i)
        {
            for (var j = i + 1; j < items.Count(); ++j)
            {
                yield return (items.ElementAt(i), items.ElementAt(j));
            }
        }
    }
}