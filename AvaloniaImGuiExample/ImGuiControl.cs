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
    public class ImGuiControl : OpenGlControlBase
    {
        private GL? _gl;
        private ImGuiController? _controller;
        private bool _frameBegun;
        private DateTime _lastFrameTime = DateTime.UtcNow;

        // Reference to the top level window for global input queries
        private TopLevel? _topLevel;

        public event Action<GL, ImGuiController>? OnImGuiRender;

        public ImGuiControl()
        {
            // Enable focus to receive keyboard input
            Focusable = true;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _topLevel = TopLevel.GetTopLevel(this);
        }

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

            // Update input state using Avalonia's global queries
            UpdateInputStateFromAvalonia();

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

        private void UpdateInputStateFromAvalonia()
        {
            if (_controller == null || _topLevel == null) return;

            var io = ImGui.GetIO();

            // Update mouse state using Avalonia's global input queries
            UpdateMouseStateFromAvalonia(io);

            // Update keyboard state using Avalonia's global input queries
            UpdateKeyboardStateFromAvalonia(io);
        }

        private void UpdateMouseStateFromAvalonia(ImGuiIOPtr io)
        {
            try
            {
                if (_topLevel != null)
                {
                    // Get global mouse position and convert to local coordinates
                    var globalMousePos = _topLevel.PointToClient(PixelPoint.Origin);
                    
                    // Try to get the actual mouse position relative to this control
                    // This is a simplified approach - we'll use the bounds to estimate position
                    var controlBounds = this.Bounds;
                    var parentBounds = (this.Parent as Control)?.Bounds ?? new Rect();
                    
                    // For now, we'll use a simple approach to get mouse position
                    // In a real implementation, you might need platform-specific code
                    var mousePos = GetMousePositionRelativeToControl();
                    if (mousePos.HasValue)
                    {
                        io.AddMousePosEvent(mousePos.Value.X, mousePos.Value.Y);
                    }

                    // Query mouse button states using Avalonia's input system
                    // Note: Avalonia doesn't provide direct global mouse button state queries
                    // We'll need to track these through events or use platform-specific APIs
                    UpdateMouseButtonStates(io);
                }
            }
            catch
            {
                // Fallback to default state
            }
        }

        private Vector2? GetMousePositionRelativeToControl()
        {
            try
            {
                if (_topLevel != null)
                {
                    // Get the current pointer position from the platform
                    // Note: Avalonia doesn't provide a direct way to get global mouse position
                    // We need to use platform-specific APIs or track through events
                    
                    // For now, we'll use a workaround by checking if we have a window
                    if (_topLevel is Window window)
                    {
                        // Try to get mouse position through the window's pointer position
                        // This is a simplified approach - in practice you might need platform-specific code
                        
                        // Get the position of this control relative to the window
                        var controlPosition = this.TranslatePoint(new Point(0, 0), window);
                        
                        if (controlPosition.HasValue)
                        {
                            // Since we can't directly get global mouse position in Avalonia,
                            // we'll return the center of the control as a fallback
                            // In a real implementation, you would use platform-specific APIs
                            return new Vector2((float)(Bounds.Width / 2), (float)(Bounds.Height / 2));
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return new Vector2(0, 0); // Default position
        }

        private void UpdateMouseButtonStates(ImGuiIOPtr io)
        {
            // Avalonia doesn't provide direct global mouse button state queries
            // We need to track these states ourselves or use platform-specific APIs
            
            // For now, we'll use a simple approach with default states
            // In a real implementation, you would query actual button states
            io.AddMouseButtonEvent(0, false); // Left button
            io.AddMouseButtonEvent(1, false); // Right button
            io.AddMouseButtonEvent(2, false); // Middle button
        }

        private void UpdateKeyboardStateFromAvalonia(ImGuiIOPtr io)
        {
            try
            {
                // Avalonia doesn't provide direct global keyboard state queries
                // We need to use platform-specific APIs or track state through events
                
                // For now, we'll use a simplified approach
                // In a real implementation, you would query the actual keyboard state
                var allKeys = Enum.GetValues<Key>();
                
                foreach (var key in allKeys)
                {
                    var imguiKey = GetImGuiKey(key);
                    if (imguiKey != ImGuiKey.None)
                    {
                        // Query key state - this is where you'd use platform-specific APIs
                        bool isPressed = GetKeyState(key);
                        io.AddKeyEvent(imguiKey, isPressed);
                    }
                }
            }
            catch
            {
                // Fallback - continue with existing state
            }
        }

        private bool GetKeyState(Key key)
        {
            // This is where you would implement platform-specific keyboard state queries
            // For now, return false as a placeholder
            // In a real implementation, you might use:
            // - Windows: GetKeyState() or GetAsyncKeyState()
            // - Linux: X11 or Wayland APIs
            // - macOS: Cocoa APIs
            return false;
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            _controller?.WindowResized((int)e.NewSize.Width, (int)e.NewSize.Height);
        }

        // Minimal event handlers for focus management
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus(); // Focus to receive keyboard input
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
            _controller?.Dispose();
            base.OnDetachedFromVisualTree(e);
        }
    }
}
