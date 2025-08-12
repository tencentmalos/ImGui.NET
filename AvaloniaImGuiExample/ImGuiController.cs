using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace AvaloniaImGuiExample
{
    public class ImGuiController : IDisposable
    {
        private readonly GL _gl;
        private uint _vertexArray;
        private uint _vertexBuffer;
        private uint _indexBuffer;
        private uint _shader;
        private uint _fontTexture;
        private int _windowWidth;
        private int _windowHeight;


        public ImGuiController(GL gl, int width, int height)
        {
            _gl = gl;
            _windowWidth = width;
            _windowHeight = height;

            // Initialize ImGui context
            ImGui.CreateContext();
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;

            // Set display size
            io.DisplaySize = new Vector2(width, height);
            io.DisplayFramebufferScale = Vector2.One;

            // Create OpenGL resources
            CreateDeviceObjects();
            CreateFontsTexture();
        }

        public void Update(float deltaTime, int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;

            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(width, height);
            io.DeltaTime = deltaTime;
        }

        public void Render(ImDrawDataPtr drawData)
        {
            if (drawData.CmdListsCount == 0)
                return;

            // Backup GL state
            _gl.GetInteger(GetPName.ActiveTexture, out int lastActiveTexture);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.GetInteger(GetPName.CurrentProgram, out int lastProgram);
            _gl.GetInteger(GetPName.TextureBinding2D, out int lastTexture);
            _gl.GetInteger(GetPName.ArrayBufferBinding, out int lastArrayBuffer);
            _gl.GetInteger(GetPName.VertexArrayBinding, out int lastVertexArray);

            // Setup render state
            _gl.Enable(EnableCap.Blend);
            _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.Disable(EnableCap.CullFace);
            _gl.Disable(EnableCap.DepthTest);
            _gl.Enable(EnableCap.ScissorTest);

            // Setup viewport
            _gl.Viewport(0, 0, (uint)_windowWidth, (uint)_windowHeight);

            // Setup projection matrix
            var orthoProjection = Matrix4x4.CreateOrthographicOffCenter(
                0.0f, _windowWidth, _windowHeight, 0.0f, -1.0f, 1.0f);

            _gl.UseProgram(_shader);
            var projectionLocation = _gl.GetUniformLocation(_shader, "ProjMtx");
            _gl.UniformMatrix4(projectionLocation, 1, false, GetMatrixFloatArray(orthoProjection));

            _gl.BindVertexArray(_vertexArray);

            // Calculate total buffer sizes needed
            uint totalVtxCount = 0;
            uint totalIdxCount = 0;
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];
                totalVtxCount += (uint)cmdList.VtxBuffer.Size;
                totalIdxCount += (uint)cmdList.IdxBuffer.Size;
            }

            // Ensure buffers are large enough
            var vtxSize = totalVtxCount * (uint)Unsafe.SizeOf<ImDrawVert>();
            var idxSize = totalIdxCount * sizeof(ushort);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, vtxSize, ReadOnlySpan<byte>.Empty, BufferUsageARB.StreamDraw);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, idxSize, ReadOnlySpan<byte>.Empty, BufferUsageARB.StreamDraw);

            // Upload data and render
            uint vtxOffset = 0;
            uint idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];

                // Upload vertex data
                var vtxBufferSize = (uint)(cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>());
                _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
                unsafe
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, (nint)vtxOffset, vtxBufferSize, (void*)cmdList.VtxBuffer.Data);
                }

                // Upload index data
                var idxBufferSize = (uint)(cmdList.IdxBuffer.Size * sizeof(ushort));
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
                unsafe
                {
                    _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, (nint)idxOffset, idxBufferSize, (void*)cmdList.IdxBuffer.Data);
                }

                // Render commands
                for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
                {
                    var cmd = cmdList.CmdBuffer[i];

                    if (cmd.UserCallback != IntPtr.Zero)
                        continue;

                    // Set scissor rectangle
                    //_gl.Scissor(0, 0, (uint)_windowWidth, (uint)_windowHeight);

                    _gl.Scissor((int)cmd.ClipRect.X,
                        (int)(_windowHeight - cmd.ClipRect.W),
                        (uint)(cmd.ClipRect.Z - cmd.ClipRect.X),
                        (uint)(cmd.ClipRect.W - cmd.ClipRect.Y));

                    // Bind texture
                    _gl.BindTexture(TextureTarget.Texture2D, (uint)cmd.TextureId);

                    // Draw
                    unsafe
                    {
                        var elementOffset = (nint)((idxOffset / sizeof(ushort)) + cmd.IdxOffset);
                        var vertexOffset = (int)(vtxOffset / (uint)Unsafe.SizeOf<ImDrawVert>()) + cmd.VtxOffset;
                        _gl.DrawElementsBaseVertex(GLEnum.Triangles, cmd.ElemCount,
                            GLEnum.UnsignedShort, (void*)(elementOffset * sizeof(ushort)),
                            (int)vertexOffset);
                    }
                }

                vtxOffset += vtxBufferSize;
                idxOffset += idxBufferSize;
            }

            // Restore GL state
            _gl.UseProgram((uint)lastProgram);
            _gl.BindTexture(TextureTarget.Texture2D, (uint)lastTexture);
            _gl.ActiveTexture((TextureUnit)lastActiveTexture);
            _gl.BindVertexArray((uint)lastVertexArray);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)lastArrayBuffer);
            _gl.Disable(EnableCap.ScissorTest);
        }


        //public void WindowResized(int width, int height)
        //{
        //    _windowWidth = width;
        //    _windowHeight = height;
        //}

        private void CreateDeviceObjects()
        {
            // Load shaders from embedded resources
            var vertexShaderSource = LoadEmbeddedShader("AvaloniaImGuiExample.Shaders.imgui_vertex.glsl");
            var fragmentShaderSource = LoadEmbeddedShader("AvaloniaImGuiExample.Shaders.imgui_fragment.glsl");

            var vertexShader = CreateShader(ShaderType.VertexShader, vertexShaderSource);
            var fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentShaderSource);

            _shader = _gl.CreateProgram();
            _gl.AttachShader(_shader, vertexShader);
            _gl.AttachShader(_shader, fragmentShader);
            _gl.LinkProgram(_shader);

            _gl.GetProgram(_shader, GLEnum.LinkStatus, out int status);
            if (status == 0)
            {
                var log = _gl.GetProgramInfoLog(_shader);
                throw new Exception($"Shader linking failed: {log}");
            }

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            // Create buffers
            _vertexArray = _gl.GenVertexArray();
            _vertexBuffer = _gl.GenBuffer();
            _indexBuffer = _gl.GenBuffer();

            _gl.BindVertexArray(_vertexArray);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);

            // Setup vertex attributes
            unsafe
            {
                _gl.EnableVertexAttribArray(0);
                _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 
                    (uint)Unsafe.SizeOf<ImDrawVert>(), (void*)0);

                _gl.EnableVertexAttribArray(1);
                _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 
                    (uint)Unsafe.SizeOf<ImDrawVert>(), (void*)8);

                _gl.EnableVertexAttribArray(2);
                _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, 
                    (uint)Unsafe.SizeOf<ImDrawVert>(), (void*)16);
            }

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
            _gl.BindVertexArray(0);
        }

        private string LoadEmbeddedShader(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded shader resource not found: {resourceName}");
            
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private uint CreateShader(ShaderType type, string source)
        {
            var shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                var log = _gl.GetShaderInfoLog(shader);
                throw new Exception($"Shader compilation failed: {log}");
            }

            return shader;
        }

        private void CreateFontsTexture()
        {
            var io = ImGui.GetIO();

            // Build texture atlas
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            // Create OpenGL texture
            _fontTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _fontTexture);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            unsafe
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 
                    0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToPointer());
            }

            // Store texture ID
            io.Fonts.SetTexID((IntPtr)_fontTexture);
            io.Fonts.ClearTexData();
        }

        private static unsafe float[] GetMatrixFloatArray(Matrix4x4 matrix)
        {
            return new float[]
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            };
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_vertexArray);
            _gl.DeleteBuffer(_vertexBuffer);
            _gl.DeleteBuffer(_indexBuffer);
            _gl.DeleteProgram(_shader);
            _gl.DeleteTexture(_fontTexture);
            ImGui.DestroyContext();
        }

        
    }
}
