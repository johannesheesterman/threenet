using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Drawing;
using System;

public class RenderContext
{
    private GL gl;
    private IWindow window;
    private uint vao;
    private uint vbo;
    private uint ebo;
    private uint program;
    
    public void Run()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "Silk.NET Windowing";

        window = Window.Create(options);

        window.Load += OnLoad;
        window.Render += OnRender;

        window.Run();
    }

    private unsafe void OnLoad()
    {
        gl = window.CreateOpenGL();
        gl.ClearColor(Color.CornflowerBlue);

        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        var vertices = new []
        {
            0.5f,  0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.0f
        };        

        fixed (float* buf = vertices)
        gl.BufferData(
            BufferTargetARB.ArrayBuffer, 
            (nuint) (vertices.Length * sizeof(float)), 
            buf, 
            BufferUsageARB.StaticDraw);

        var indices = new []
        {
            0u, 1u, 3u,
            1u, 2u, 3u
        };

        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

        fixed (uint* buf = indices)
        gl.BufferData(
            BufferTargetARB.ElementArrayBuffer, 
            (nuint) (indices.Length * sizeof(uint)), 
            buf, 
            BufferUsageARB.StaticDraw);

        const string vertexCode = @"
#version 330 core

layout (location = 0) in vec3 aPosition;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
}";

        const string fragmentCode = @"
#version 330 core

out vec4 out_color;

void main()
{
    out_color = vec4(1.0, 0.5, 0.2, 1.0);
}";

        var vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexCode);
        gl.CompileShader(vertexShader);

        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatus);
        if (vStatus != (int) GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + gl.GetShaderInfoLog(vertexShader));

        var fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentCode);
        gl.CompileShader(fragmentShader);

        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out var fStatus);
        if (fStatus != (int) GLEnum.True)
            throw new Exception("Fragment shader failed to compile: " + gl.GetShaderInfoLog(fragmentShader));

        program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader);
        gl.AttachShader(program, fragmentShader);
        gl.LinkProgram(program);

        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var pStatus);
        if (pStatus != (int) GLEnum.True)
            throw new Exception("Program failed to link: " + gl.GetProgramInfoLog(program));

        // After linking, we can detach and delete the shaders to free up memory
        gl.DetachShader(program, vertexShader);
        gl.DetachShader(program, fragmentShader);
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);

        const uint positionLoc = 0;
        gl.EnableVertexAttribArray(positionLoc);
        gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*) 0);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);        
    }

    private unsafe void OnRender(double dt)
    {
        gl!.Clear(ClearBufferMask.ColorBufferBit);

        gl.BindVertexArray(vao);
        gl.UseProgram(program);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*) 0);
    }
}