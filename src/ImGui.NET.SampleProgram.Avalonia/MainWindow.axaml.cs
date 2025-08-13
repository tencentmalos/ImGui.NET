using System;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using ImGuiNET;
using Silk.NET.OpenGL;

namespace AvaloniaImGuiExample
{
    public partial class MainWindow : Window
    {
        // Demo state variables
        private bool _showDemoWindow = true;
        private bool _showAnotherWindow = false;
        private float _floatValue = 0.0f;
        private int _counter = 0;
        private Vector3 _clearColor = new(0.45f, 0.55f, 0.6f);
        private string _textInput = "Hello, ImGui!";

        public MainWindow()
        {
            InitializeComponent();
            
            // Subscribe to ImGui render event
            ImGuiControl.OnImGuiRender += OnImGuiRender;
        }

        //protected override void OnPointerPressed(PointerPressedEventArgs e)
        //{
        //    base.OnPointerPressed(e);
        //}

        private void OnImGuiRender(GL gl, ImGuiController controller)
        {
            // Clear background
            gl.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, 1.0f);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            // Demo window
            if (_showDemoWindow)
            {
                ImGui.ShowDemoWindow(ref _showDemoWindow);
            }

            // Main control window
            ImGui.Begin("Avalonia ImGui Control Demo");
            
            ImGui.Text("Hello from Avalonia + ImGui!");
            ImGui.Text($"Application average {1000.0f / ImGui.GetIO().Framerate:0.##} ms/frame ({ImGui.GetIO().Framerate:0.#} FPS)");
            
            ImGui.Separator();
            
            // Controls
            ImGui.Checkbox("Show Demo Window", ref _showDemoWindow);
            ImGui.Checkbox("Show Another Window", ref _showAnotherWindow);
            
            ImGui.SliderFloat("Float Value", ref _floatValue, 0.0f, 1.0f);
            ImGui.ColorEdit3("Clear Color", ref _clearColor);
            
            ImGui.InputText("Text Input", ref _textInput, 256);
            
            if (ImGui.Button("Button"))
            {
                _counter++;
            }
            ImGui.SameLine();
            ImGui.Text($"counter = {_counter}");
            
            ImGui.End();

            // Another window
            if (_showAnotherWindow)
            {
                ImGui.Begin("Another Window", ref _showAnotherWindow);
                ImGui.Text("Hello from another window!");
                ImGui.Text($"Float value: {_floatValue:F3}");
                ImGui.Text($"Text input: {_textInput}");
                
                if (ImGui.Button("Close Me"))
                {
                    _showAnotherWindow = false;
                }
                ImGui.End();
            }

            // Performance window
            ImGui.Begin("Performance");
            ImGui.Text($"Mouse Position: {ImGui.GetMousePos()}");
            ImGui.Text($"Display Size: {ImGui.GetIO().DisplaySize}");
            ImGui.Text($"Delta Time: {ImGui.GetIO().DeltaTime:F6}s");
            
            // Plot some values
            var values = new float[100];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = MathF.Sin(i * 0.1f + (float)DateTime.Now.TimeOfDay.TotalSeconds);
            }
            ImGui.PlotLines("Sine Wave", ref values[0], values.Length, 0, null, -1.0f, 1.0f, new Vector2(0, 80));
            
            ImGui.End();

            // Style editor
            ImGui.Begin("Style Editor");
            ImGui.ShowStyleEditor();
            ImGui.End();
        }

    }
}
