using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GamePlayer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StbImageSharp;
using BeginMode = OpenTK.Graphics.OpenGL4.BeginMode;
using ClearBufferMask = OpenTK.Graphics.OpenGL4.ClearBufferMask;
using DrawElementsType = OpenTK.Graphics.OpenGL4.DrawElementsType;
using GL = OpenTK.Graphics.OpenGL4.GL;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using PixelType = OpenTK.Graphics.OpenGL4.PixelType;
using TextureMagFilter = OpenTK.Graphics.OpenGL4.TextureMagFilter;
using TextureMinFilter = OpenTK.Graphics.OpenGL4.TextureMinFilter;
using TextureParameterName = OpenTK.Graphics.OpenGL4.TextureParameterName;
using TextureTarget = OpenTK.Graphics.OpenGL4.TextureTarget;
using TextureWrapMode = OpenTK.Graphics.OpenGL4.TextureWrapMode;
using VertexAttribPointerType = OpenTK.Graphics.OpenGL4.VertexAttribPointerType;

public class GameView : GameWindow
{
    private const int FrameRate = 30;

    private readonly LevelData _levelData;

    private Shader _tileShader;
    private Shader _playerShader;
    private int _vertexBuffer;
    private int _vertexArrayObject;
    private int _element;
    private Dictionary<char, Tile>? _tiles;
    private Matrix4 _projectionMatrix;
    private Matrix4 _viewMatrix;
    private PlayerSprite _playerSprite;
    private GameStateReducer _gameStateReducer;
    private int _frame;
    private int _frameCount;
    private int _recordingFinishedFrame = int.MaxValue - 1000;

    private GameState _gameState = new (new (new Vector2(5.0f, 10.0f), new Vector2(0.0f, 0.0f), false), 16f, true);

    private readonly float[] _squareVertexes = 
    {
         8.0f,  8.0f, 0.0f, 1.0f, 1.0f,
         8.0f, -8.0f, 0.0f, 1.0f, 0.0f,
        -8.0f, -8.0f, 0.0f, 0.0f, 0.0f,
        -8.0f,  8.0f, 0.0f, 0.0f, 1.0f,
    };

    private readonly uint[] _squareVertexIndexes =
    {
        0, 1, 3,
        1, 2, 3,
    };

    private readonly IEnumerator<InputState> _recording;

    public GameView(LevelData levelData) : this(levelData, Array.Empty<InputState>())
    {
    }

    public GameView(LevelData levelData, IEnumerable<InputState> recording)
        : base(
            new GameWindowSettings { RenderFrequency = FrameRate, UpdateFrequency = FrameRate },
            new NativeWindowSettings { Size = (800, 600), Title = "Jumpman" })
    {
        _levelData = levelData;
        _recording = recording.GetEnumerator();

        if (!Directory.Exists("outputimg"))
            Directory.CreateDirectory("outputimg");

        foreach (var path in Directory.GetFiles("outputimg", "*.png"))
        {
            File.Delete(path);
        }

        File.WriteAllText(@"outputimg\input.txt", "");

        _frameCount = 0;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.BindVertexArray(_vertexArrayObject);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        _tileShader.Use();
        _tileShader.SetMatrix("model", Matrix4.Identity);
        _tileShader.SetMatrix("projection", _projectionMatrix);

        _gameState = _gameState with
        {
            HorizontalScroll = MathF.Max(_gameState.HorizontalScroll, (_gameState.CharacterDetails.Position.X - 11) * 16f)
        };

        var xOffset = (int)MathF.Floor(_gameState.HorizontalScroll / 16f);
        var viewMatrix = _viewMatrix * Matrix4.CreateTranslation(MathF.Floor(-_gameState.HorizontalScroll + xOffset * 16f), 0f, 0f);
        _tileShader.SetMatrix("view", viewMatrix);

        for (var y = 0; y < 16; ++y)
        {
            for (var x = 0; x < 22; ++x)
            {
                var tileKey = _levelData![x + xOffset, y];

                if (!_tiles!.ContainsKey(tileKey)) continue;

                _tiles![tileKey].Use();

                _tileShader.SetMatrix("model", Matrix4.CreateTranslation(-160f + 16f * x, 120f - 16f * y, 0f));

                GL.DrawElements(BeginMode.Triangles, _squareVertexIndexes.Length, DrawElementsType.UnsignedInt, 0);
            }
        }

        _frame = (_frame + 1) % 10;

        _playerShader.Use();
        _playerSprite.Use();

        _playerShader.SetMatrix("view", viewMatrix);
        _playerShader.SetMatrix("projection", _projectionMatrix);
        _playerShader.SetMatrix("model",
            Matrix4.CreateScale(_gameState.CharacterDetails.Velocity.X < 0.0f ? -1f : 1f, 1f, 1f) *
            Matrix4.CreateTranslation(-160f + 16f * (_gameState.CharacterDetails.Position.X - xOffset), 120f - 16f * _gameState.CharacterDetails.Position.Y, 0f));

        _playerShader.SetValue("frame", _gameState.CharacterDetails.Velocity.X switch
        {
            < 0f or > 0f when _gameState.CharacterDetails.IsGrounded => 2 + _frame / 5,
            _ when !_gameState.CharacterDetails.IsGrounded => 3,
            _ => 0
        });

        GL.DrawElements(BeginMode.Triangles, _squareVertexIndexes.Length, DrawElementsType.UnsignedInt, 0);

        //var stride = Size.X * 3;
        //stride += (4 - stride % 4) % 4;
        //var pixels = new byte[stride * Size.Y];
        //GL.ReadPixels(0, 0, Size.X, Size.Y, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
        //using var image = Image.LoadPixelData<Rgb24>(Configuration.Default, pixels, Size.X, Size.Y);
        //image.Mutate(x => x.Flip(FlipMode.Vertical));
        //var imageFileName = $"{_frameCount++}.png";
        //image.Save($"outputimg/{imageFileName}");
        //File.AppendAllText(@"outputimg\input.txt", $"file '{imageFileName}'\nduration {args.Time}\n");
        
        //Title = $"{_frameCount}";

        Context.SwapBuffers();

        //if (_frameCount > _recordingFinishedFrame + 90)
        //    Close();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);


        if (_recording.MoveNext())
        {
            _gameState = _gameStateReducer.Reduce(_gameState, _recording.Current);
        }
        else
        {
            _recordingFinishedFrame = Math.Min(_recordingFinishedFrame, _frameCount);

            _gameState = _gameStateReducer.Reduce(_gameState, new InputState(
                KeyboardState.IsKeyDown(Keys.D) ? LeftRightStatus.Right
                : KeyboardState.IsKeyDown(Keys.A) ? LeftRightStatus.Left
                : LeftRightStatus.None,
                KeyboardState.IsKeyPressed(Keys.Space)));
        }
    }

    protected override void OnLoad()
    {
        _gameStateReducer = new GameStateReducer(_levelData);

        _playerSprite = PlayerSprite.FromFile(@".\Tiles\player.png");

        GL.ClearColor(0.34f, 0.58f, 0.98f, 1.0f);

        _tileShader = Shader.FromFiles("basic.vert", "basic.frag");
        _playerShader = Shader.FromFiles("player.vert", "player.frag");

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _squareVertexes.Length * sizeof(float), _squareVertexes, BufferUsageHint.StaticDraw);

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);

        var vertexAttribute = _tileShader.GetAttributeLocation("vPos");
        GL.VertexAttribPointer(vertexAttribute, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        var tileTextureAttribute = _tileShader.GetAttributeLocation("aTexCoord");
        GL.EnableVertexAttribArray(tileTextureAttribute);
        GL.VertexAttribPointer(tileTextureAttribute, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        var playerTextureAttribute = _playerShader.GetAttributeLocation("aTexCoord");
        GL.EnableVertexAttribArray(playerTextureAttribute);
        GL.VertexAttribPointer(playerTextureAttribute, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        _element = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _element);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _squareVertexIndexes.Length * sizeof(uint), _squareVertexIndexes, BufferUsageHint.StaticDraw);

        _tiles = Directory.GetFiles(@".\Tiles", "?.png")
            .Select(Tile.FromFile)
            .ToTileDictionary();

        _projectionMatrix = Matrix4.CreateOrthographic(336, 256, 0.1f, 10.0f);
        _viewMatrix = Matrix4.CreateTranslation(0f, 0f, -3.0f);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _tileShader.Dispose();
    }
}

public record InputState(LeftRightStatus LeftRightStatus, bool JumpPressed);

[Flags]
public enum LeftRightStatus
{
    None = 0b00,
    Left = 0b01,
    Right = 0b10,
}

public record CharacterDetails(Vector2 Position, Vector2 Velocity, bool IsGrounded);

public record GameState(
    CharacterDetails CharacterDetails,
    float HorizontalScroll,
    bool IsAlive);