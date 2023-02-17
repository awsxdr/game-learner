namespace GamePlayer;

using System.IO;
using System.Linq;
using StbImageSharp;

public class Tile : Texture
{
    public char Name { get; }

    private Tile(char name, ImageResult image) : base(image)
    {
        Name = name;
    }

    public static Tile FromFile(string filePath)
    {
        var image = ImageFromFile(filePath);
        return new Tile(Path.GetFileNameWithoutExtension(filePath).Single(), image);
    }
}