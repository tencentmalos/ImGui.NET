# Avalonia ImGui Example

This project demonstrates how to integrate ImGui.NET with Avalonia UI using the OpenGlControlBase.

## Features

- **ImGuiControl**: A custom Avalonia control that inherits from `OpenGlControlBase`
- **ImGuiController**: A renderer that uses Silk.NET OpenGL to render ImGui
- **Shader Support**: Separate GLSL shader files for vertex and fragment shaders
- **Input Handling**: Complete mouse and keyboard input support
- **Demo Application**: Shows various ImGui features including:
  - Demo window with all ImGui widgets
  - Custom control windows
  - Performance monitoring
  - Style editor
  - Real-time plotting

## Architecture

### ImGuiControl
- Inherits from `OpenGlControlBase`
- Handles OpenGL initialization and rendering
- Manages input events (mouse, keyboard, text)
- Provides an event for custom ImGui rendering logic

### ImGuiController
- Manages ImGui context and rendering
- Uses Silk.NET OpenGL for low-level graphics operations
- Loads shaders from embedded resources
- Handles font texture creation and management

### Shader Files
- `imgui_vertex.glsl`: Vertex shader for ImGui rendering
- `imgui_fragment.glsl`: Fragment shader for ImGui rendering
- Embedded as resources in the assembly

## Usage

1. Add the `ImGuiControl` to your Avalonia window
2. Subscribe to the `OnImGuiRender` event
3. Implement your ImGui UI in the event handler

```csharp
// In your window constructor
ImGuiControl.OnImGuiRender += OnImGuiRender;

// Event handler
private void OnImGuiRender(GL gl, ImGuiController controller)
{
    // Clear background
    gl.ClearColor(0.45f, 0.55f, 0.6f, 1.0f);
    gl.Clear(ClearBufferMask.ColorBufferBit);

    // Your ImGui code here
    ImGui.Begin("My Window");
    ImGui.Text("Hello, ImGui!");
    ImGui.End();
}
```

## Dependencies

- Avalonia 11.0.10
- ImGui.NET 1.90.4.1
- Silk.NET.OpenGL 2.20.0

## Building and Running

```bash
cd AvaloniaImGuiExample
dotnet restore
dotnet run
```

## Platform Support

This example is configured to use OpenGL rendering on all supported platforms:
- Windows: WGL (Windows OpenGL)
- Linux: GLX (X11 OpenGL)
- macOS: Native OpenGL

## Key Implementation Details

1. **OpenGL Context**: Uses Avalonia's OpenGL integration through `OpenGlControlBase`
2. **Shader Loading**: Shaders are embedded as resources and loaded at runtime
3. **Input Mapping**: Avalonia input events are mapped to ImGui input events
4. **Render Loop**: Uses `DispatcherTimer` for consistent 60 FPS rendering
5. **Resource Management**: Proper disposal of OpenGL resources and ImGui context

## Extending the Example

To add your own ImGui content:

1. Create state variables in your window class
2. Add ImGui rendering code in the `OnImGuiRender` method
3. Handle any additional input events if needed

The example provides a solid foundation for building complex ImGui-based tools and editors within Avalonia applications.
