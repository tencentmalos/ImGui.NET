using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace AvaloniaImGuiExample;

public partial class ucImGui : UserControl
{
    public event Action<GL, ImGuiController>? OnImGuiRender
    {
        add => _nativeControl!.OnImGuiRender += value;
        remove => _nativeControl!.OnImGuiRender -= value;
    }

    public ucImGui()
    {
        InitializeComponent();

        // Enable focus to receive keyboard input
        Focusable = true;

        // Subscribe to input events with tunneling (preview) events for higher priority
        this.AddHandler(PointerPressedEvent, HandlePointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.AddHandler(PointerReleasedEvent, HandlePointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.AddHandler(PointerMovedEvent, HandlePointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.AddHandler(PointerWheelChangedEvent, HandlePointerWheelChanged, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.AddHandler(KeyDownEvent, HandleKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.AddHandler(KeyUpEvent, HandleKeyUp, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        this.AddHandler(TextInputEvent, HandleTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
    }


    private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Focus to receive keyboard input
        Focus();

        if (_nativeControl != null)
        {
            var position = e.GetPosition(_nativeControl);
            _nativeControl.InjectMousePosition(new Vector2((float)position.X, (float)position.Y));

            var properties = e.GetCurrentPoint(_nativeControl).Properties;

            if (properties.IsLeftButtonPressed)
                _nativeControl.InjectMouseButton(0, true);
            if (properties.IsRightButtonPressed)
                _nativeControl.InjectMouseButton(1, true);
            if (properties.IsMiddleButtonPressed)
                _nativeControl.InjectMouseButton(2, true);
        }

        e.Handled = true;
    }

    private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_nativeControl != null)
        {
            var position = e.GetPosition(_nativeControl);
            _nativeControl.InjectMousePosition(new Vector2((float)position.X, (float)position.Y));

            var properties = e.GetCurrentPoint(_nativeControl).Properties;

            // Check which button was released
            if (e.InitialPressMouseButton == MouseButton.Left)
                _nativeControl.InjectMouseButton(0, false);
            else if (e.InitialPressMouseButton == MouseButton.Right)
                _nativeControl.InjectMouseButton(1, false);
            else if (e.InitialPressMouseButton == MouseButton.Middle)
                _nativeControl.InjectMouseButton(2, false);
        }

        e.Handled = true;
    }

    private void HandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_nativeControl != null)
        {
            var position = e.GetPosition(_nativeControl);
            _nativeControl.InjectMousePosition(new Vector2((float)position.X, (float)position.Y));

            // Update button states during move
            var properties = e.GetCurrentPoint(_nativeControl).Properties;
            _nativeControl.InjectMouseButton(0, properties.IsLeftButtonPressed);
            _nativeControl.InjectMouseButton(1, properties.IsRightButtonPressed);
            _nativeControl.InjectMouseButton(2, properties.IsMiddleButtonPressed);
        }

        e.Handled = true;
    }

    private void HandlePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_nativeControl != null)
        {
            var position = e.GetPosition(_nativeControl);
            _nativeControl.InjectMousePosition(new Vector2((float)position.X, (float)position.Y));
            _nativeControl.InjectMouseWheel((float)e.Delta.Y);
        }

        e.Handled = true;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (_nativeControl != null)
        {
            _nativeControl.InjectKeyState(e.Key, true);
        }

        e.Handled = true;
    }

    private void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        if (_nativeControl != null)
        {
            _nativeControl.InjectKeyState(e.Key, false);
        }

        e.Handled = true;
    }

    private void HandleTextInput(object? sender, TextInputEventArgs e)
    {
        if (_nativeControl != null && !string.IsNullOrEmpty(e.Text))
        {
            _nativeControl.InjectTextInput(e.Text);
        }

        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        HandlePointerPressed(this, e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        HandlePointerReleased(this, e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        HandlePointerMoved(this, e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        HandlePointerWheelChanged(this, e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        HandleKeyDown(this, e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        HandleKeyUp(this, e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        HandleTextInput(this, e);
    }

    // Handle pointer enter/leave for proper mouse tracking
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        if (_nativeControl != null)
        {
            var position = e.GetPosition(_nativeControl);
            _nativeControl.InjectMousePosition(new Vector2((float)position.X, (float)position.Y));
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (_nativeControl != null)
        {
            // Set mouse position outside the control bounds
            _nativeControl.InjectMousePosition(new Vector2(-1, -1));

            // Release all mouse buttons when pointer exits
            _nativeControl.InjectMouseButton(0, false);
            _nativeControl.InjectMouseButton(1, false);
            _nativeControl.InjectMouseButton(2, false);
        }
    }

    // Handle focus events to manage keyboard input
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        _nativeControl?.Focus();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        if (_nativeControl != null)
        {
            // Clear all key states when losing focus
            foreach (Key key in Enum.GetValues<Key>())
            {
                _nativeControl.InjectKeyState(key, false);
            }

            // Clear mouse button states
            _nativeControl.InjectMouseButton(0, false);
            _nativeControl.InjectMouseButton(1, false);
            _nativeControl.InjectMouseButton(2, false);
        }
    }

}