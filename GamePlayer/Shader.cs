namespace GamePlayer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

public class Shader : IDisposable
{
    private bool _hasDisposed = false;
    private readonly Dictionary<string, int> _uniformLocations;

    public int Handle { get; }

    private Shader(int programHandle)
    {
        Handle = programHandle;

        GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

        _uniformLocations =
            Enumerable.Range(0, numberOfUniforms)
            .Select(i => GL.GetActiveUniform(Handle, i, out _, out _))
            .ToDictionary(
                k => k,
                k => GL.GetUniformLocation(Handle, k));
    }

    ~Shader()
    {
        GL.DeleteShader(Handle);
    }

    public static Shader FromFiles(string vertexShaderPath, string fragmentShaderPath)
    {
        var vertexShader = LoadShader(vertexShaderPath, ShaderType.VertexShader);
        var fragmentShader = LoadShader(fragmentShaderPath, ShaderType.FragmentShader);

        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);

        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var success);

        if (success == 0)
        {
            throw new Exception(GL.GetProgramInfoLog(program));
        }

        return new(program);
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    private static int LoadShader(string path, ShaderType type)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, File.ReadAllText(path));

        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var success);

        if (success == 0)
        {
            throw new Exception(GL.GetShaderInfoLog(shader));
        }

        return shader;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_hasDisposed)
        {
            GL.DeleteProgram(Handle);

            _hasDisposed = true;
        }
    }

    public int GetAttributeLocation(string attributeName) =>
        GL.GetAttribLocation(Handle, attributeName);

    public void SetMatrix(string matrixName, Matrix4 matrix) =>
        GL.ProgramUniformMatrix4(Handle, _uniformLocations[matrixName], true, ref matrix);

    public void SetValue(string valueName, int value) =>
        GL.ProgramUniform1(Handle, _uniformLocations[valueName], value);
}