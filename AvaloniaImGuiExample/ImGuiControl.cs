using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace AvaloniaImGuiExample
{
    public class ImGuiControl : OpenGlControlBase
    {
        private GL? _gl;
        private ImGuiController? _controller;
        private bool _frameBegun;
        private DateTime _lastFrameTime = DateTime.UtcNow;

        public event Action<GL, ImGuiController>? OnImGuiRender;

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
            
            // Create Silk.NET OpenGL context
            _gl = GL.GetApi(gl.GetProcAddress);
            
            // Initialize ImGui controller
            _controller = new ImGuiController(_gl, (int)Bounds.Width, (int)Bounds.Height);
            
            // Setup render loop
            DispatcherTimer.Run(() =>
            {
                RequestNextFrameRendering();
                return true;
            }, TimeSpan.FromMilliseconds(16)); // ~60 FPS
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (_gl == null || _controller == null)
                return;

            var now = DateTime.UtcNow;
            var deltaTime = (float)(now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            // Update ImGui
            _controller.Update(deltaTime, (int)Bounds.Width, (int)Bounds.Height);

            // Begin new frame
            ImGui.NewFrame();
            _frameBegun = true;

            // Call user-defined render logic
            OnImGuiRender?.Invoke(_gl, _controller);

            // Render ImGui
            if (_frameBegun)
            {
                ImGui.Render();
                _controller.Render(ImGui.GetDrawData());
                _frameBegun = false;
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            _controller?.WindowResized((int)e.NewSize.Width, (int)e.NewSize.Height);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_controller != null)
            {
                var position = e.GetPosition(this);
                _controller.UpdateMousePosition((float)position.X, (float)position.Y);
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (_controller != null)
            {
                var button = GetImGuiMouseButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind);
                if (button.HasValue)
                {
                    _controller.UpdateMouseButton(button.Value, true);
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_controller != null)
            {
                var button = GetImGuiMouseButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind);
                if (button.HasValue)
                {
                    _controller.UpdateMouseButton(button.Value, false);
                }
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            _controller?.UpdateMouseWheel((float)e.Delta.Y);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_controller != null)
            {
                var imguiKey = GetImGuiKey(e.Key);
                if (imguiKey != ImGuiKey.None)
                {
                    _controller.UpdateKeyState(imguiKey, true);
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (_controller != null)
            {
                var imguiKey = GetImGuiKey(e.Key);
                if (imguiKey != ImGuiKey.None)
                {
                    _controller.UpdateKeyState(imguiKey, false);
                }
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            if (_controller != null && !string.IsNullOrEmpty(e.Text))
            {
                foreach (char c in e.Text)
                {
                    _controller.AddInputCharacter(c);
                }
            }
        }

        private static ImGuiMouseButton? GetImGuiMouseButton(PointerUpdateKind kind)
        {
            return kind switch
            {
                PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => ImGuiMouseButton.Left,
                PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => ImGuiMouseButton.Right,
                PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => ImGuiMouseButton.Middle,
                _ => null
            };
        }

        private static ImGuiKey GetImGuiKey(Key key)
        {
            return key switch
            {
                Key.Tab => ImGuiKey.Tab,
                Key.Left => ImGuiKey.LeftArrow,
                Key.Right => ImGuiKey.RightArrow,
                Key.Up => ImGuiKey.UpArrow,
                Key.Down => ImGuiKey.DownArrow,
                Key.PageUp => ImGuiKey.PageUp,
                Key.PageDown => ImGuiKey.PageDown,
                Key.Home => ImGuiKey.Home,
                Key.End => ImGuiKey.End,
                Key.Insert => ImGuiKey.Insert,
                Key.Delete => ImGuiKey.Delete,
                Key.Back => ImGuiKey.Backspace,
                Key.Space => ImGuiKey.Space,
                Key.Enter => ImGuiKey.Enter,
                Key.Escape => ImGuiKey.Escape,
                Key.LeftCtrl => ImGuiKey.LeftCtrl,
                Key.LeftShift => ImGuiKey.LeftShift,
                Key.LeftAlt => ImGuiKey.LeftAlt,
                Key.RightCtrl => ImGuiKey.RightCtrl,
                Key.RightShift => ImGuiKey.RightShift,
                Key.RightAlt => ImGuiKey.RightAlt,
                Key.A => ImGuiKey.A,
                Key.C => ImGuiKey.C,
                Key.V => ImGuiKey.V,
                Key.X => ImGuiKey.X,
                Key.Y => ImGuiKey.Y,
                Key.Z => ImGuiKey.Z,
                _ => ImGuiKey.None
            };
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _controller?.Dispose();
            base.OnDetachedFromVisualTree(e);
        }
    }
}
