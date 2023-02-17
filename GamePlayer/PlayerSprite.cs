namespace GamePlayer;

using StbImageSharp;

public class PlayerSprite : Texture
{
    private PlayerSprite(ImageResult image) : base(image)
    {
    }

    public static PlayerSprite FromFile(string filePath)
    {
        var image = ImageFromFile(filePath);
        return new PlayerSprite(image);
    }

    public void Use(int frame)
    {
        base.Use();
    }

    public override void Use() => Use(0);
}