namespace GamePlayer;

using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

public abstract class Texture
{
    protected ImageResult Image { get; }

    protected Texture(ImageResult image)
    {
        Image = image;
    }

    protected static ImageResult ImageFromFile(string filePath)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }

    public virtual void Use() =>
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            Image.Width,
            Image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            Image.Data);
}