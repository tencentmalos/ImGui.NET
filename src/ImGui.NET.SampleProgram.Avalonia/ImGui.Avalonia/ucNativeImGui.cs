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
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaImGuiExample
{
    public class ucNativeImGui : OpenGlControlBase
    {
        private GL? _gl;
        private ImGuiController? _controller;
        private bool _frameBegun;
        private DateTime _lastFrameTime = DateTime.UtcNow;

        // Input state tracking
        private readonly Dictionary<Key, bool> _keyStates = new();
        private readonly Dictionary<int, bool> _mouseButtonStates = new();
        private Vector2 _mousePosition = Vector2.Zero;
        private float _mouseWheel = 0f;

        public event Action<GL, ImGuiController>? OnImGuiRender;

        public double ToNativeDpiScale => VisualRoot.RenderScaling;

        public int NativePixelWidth => (int)(Bounds.Width * ToNativeDpiScale);
        public int NativePixelHeight => (int)(Bounds.Height * ToNativeDpiScale);


        public ucNativeImGui()
        {
            // Enable focus to receive keyboard input
            Focusable = false;
        }


        protected double ToNativeLength(double original)
        {
            return original * ToNativeDpiScale;
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
            
            // Create Silk.NET OpenGL context
            _gl = GL.GetApi(gl.GetProcAddress);
            
            // Initialize ImGui controller
            _controller = new ImGuiController(_gl, NativePixelWidth, NativePixelHeight);
            
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

            // Update input state
            UpdateInputState();

            // Update ImGui
            _controller.Update(deltaTime, NativePixelWidth, NativePixelHeight);

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

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            _controller?.Dispose();
            _controller = null;

            base.OnOpenGlDeinit(gl);
        }

        private void UpdateInputState()
        {
            if (_controller == null) return;

            var io = ImGui.GetIO();

            // Update mouse state
            io.AddMousePosEvent(_mousePosition.X, _mousePosition.Y);
            
            foreach (var kvp in _mouseButtonStates)
            {
                io.AddMouseButtonEvent(kvp.Key, kvp.Value);
            }

            if (_mouseWheel != 0f)
            {
                io.AddMouseWheelEvent(0f, _mouseWheel);
                _mouseWheel = 0f; // Reset wheel delta
            }

            // Update keyboard state
            foreach (var kvp in _keyStates)
            {
                var imguiKey = GetImGuiKey(kvp.Key);
                if (imguiKey != ImGuiKey.None)
                {
                    io.AddKeyEvent(imguiKey, kvp.Value);
                }
            }
        }

        // Public methods for input injection from wrapper control
        public void InjectMousePosition(Vector2 position)
        {
            _mousePosition.X = (float)ToNativeLength(position.X);
            _mousePosition.Y = (float)ToNativeLength(position.Y);
            //_mousePosition = position;
        }

        public void InjectMouseButton(int button, bool pressed)
        {
            _mouseButtonStates[button] = pressed;
        }

        public void InjectMouseWheel(float delta)
        {
            _mouseWheel += delta;
        }

        public void InjectKeyState(Key key, bool pressed)
        {
            _keyStates[key] = pressed;
        }

        public void InjectTextInput(string text)
        {
            if (_controller == null) return;
            
            var io = ImGui.GetIO();
            foreach (char c in text)
            {
                io.AddInputCharacter(c);
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);

            //Do not need here
            //_controller?.WindowResized((int)(e.NewSize.Width * dpiScale), (int)(e.NewSize.Height * dpiScale));
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
                // Number keys
                Key.D0 => ImGuiKey._0,
                Key.D1 => ImGuiKey._1,
                Key.D2 => ImGuiKey._2,
                Key.D3 => ImGuiKey._3,
                Key.D4 => ImGuiKey._4,
                Key.D5 => ImGuiKey._5,
                Key.D6 => ImGuiKey._6,
                Key.D7 => ImGuiKey._7,
                Key.D8 => ImGuiKey._8,
                Key.D9 => ImGuiKey._9,
                // Letter keys
                Key.A => ImGuiKey.A,
                Key.B => ImGuiKey.B,
                Key.C => ImGuiKey.C,
                Key.D => ImGuiKey.D,
                Key.E => ImGuiKey.E,
                Key.F => ImGuiKey.F,
                Key.G => ImGuiKey.G,
                Key.H => ImGuiKey.H,
                Key.I => ImGuiKey.I,
                Key.J => ImGuiKey.J,
                Key.K => ImGuiKey.K,
                Key.L => ImGuiKey.L,
                Key.M => ImGuiKey.M,
                Key.N => ImGuiKey.N,
                Key.O => ImGuiKey.O,
                Key.P => ImGuiKey.P,
                Key.Q => ImGuiKey.Q,
                Key.R => ImGuiKey.R,
                Key.S => ImGuiKey.S,
                Key.T => ImGuiKey.T,
                Key.U => ImGuiKey.U,
                Key.V => ImGuiKey.V,
                Key.W => ImGuiKey.W,
                Key.X => ImGuiKey.X,
                Key.Y => ImGuiKey.Y,
                Key.Z => ImGuiKey.Z,
                // Function keys
                Key.F1 => ImGuiKey.F1,
                Key.F2 => ImGuiKey.F2,
                Key.F3 => ImGuiKey.F3,
                Key.F4 => ImGuiKey.F4,
                Key.F5 => ImGuiKey.F5,
                Key.F6 => ImGuiKey.F6,
                Key.F7 => ImGuiKey.F7,
                Key.F8 => ImGuiKey.F8,
                Key.F9 => ImGuiKey.F9,
                Key.F10 => ImGuiKey.F10,
                Key.F11 => ImGuiKey.F11,
                Key.F12 => ImGuiKey.F12,
                // Keypad
                Key.NumPad0 => ImGuiKey.Keypad0,
                Key.NumPad1 => ImGuiKey.Keypad1,
                Key.NumPad2 => ImGuiKey.Keypad2,
                Key.NumPad3 => ImGuiKey.Keypad3,
                Key.NumPad4 => ImGuiKey.Keypad4,
                Key.NumPad5 => ImGuiKey.Keypad5,
                Key.NumPad6 => ImGuiKey.Keypad6,
                Key.NumPad7 => ImGuiKey.Keypad7,
                Key.NumPad8 => ImGuiKey.Keypad8,
                Key.NumPad9 => ImGuiKey.Keypad9,
                Key.Decimal => ImGuiKey.KeypadDecimal,
                Key.Divide => ImGuiKey.KeypadDivide,
                Key.Multiply => ImGuiKey.KeypadMultiply,
                Key.Subtract => ImGuiKey.KeypadSubtract,
                Key.Add => ImGuiKey.KeypadAdd,
                // Punctuation
                Key.OemSemicolon => ImGuiKey.Semicolon,
                Key.OemPlus => ImGuiKey.Equal,
                Key.OemComma => ImGuiKey.Comma,
                Key.OemMinus => ImGuiKey.Minus,
                Key.OemPeriod => ImGuiKey.Period,
                Key.OemQuestion => ImGuiKey.Slash,
                Key.OemTilde => ImGuiKey.GraveAccent,
                Key.OemOpenBrackets => ImGuiKey.LeftBracket,
                Key.OemPipe => ImGuiKey.Backslash,
                Key.OemCloseBrackets => ImGuiKey.RightBracket,
                Key.OemQuotes => ImGuiKey.Apostrophe,
                // Other keys
                Key.CapsLock => ImGuiKey.CapsLock,
                Key.NumLock => ImGuiKey.NumLock,
                Key.Scroll => ImGuiKey.ScrollLock,
                Key.PrintScreen => ImGuiKey.PrintScreen,
                Key.Pause => ImGuiKey.Pause,
                _ => ImGuiKey.None
            };
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            //_controller?.Dispose();
            base.OnDetachedFromVisualTree(e);
        }
    }
}
