using Ge.Behaviors;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using System;
using Ge.Graphics;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Immutable;

namespace Ge.Editor
{
    public class DebugConsole : Behavior
    {
        private ImmutableList<string> _lines = ImmutableList<string>.Empty;
        private TextInputBuffer _inputBuffer = new TextInputBuffer(1024);
        private bool _windowOpen;
        private GraphicsSystem _gs;
        private int _previousFrameLines;
        private bool _focusInput;

        protected override void Start(SystemRegistry registry)
        {
            registry.GetSystem<InputSystem>().RegisterCallback(input =>
            {
                if (input.GetKeyDown(Veldrid.Platform.Key.Tilde))
                {
                    _windowOpen = !_windowOpen;
                    if (_windowOpen)
                    {
                        _focusInput = true;
                    }
                }
            });

            _gs = registry.GetSystem<GraphicsSystem>();
        }

        public unsafe override void Update(float deltaSeconds)
        {
            if (_windowOpen)
            {
                var window = _gs.Context.Window;
                float width = window.Width / window.ScaleFactor.X;
                float height = window.Height / window.ScaleFactor.Y;
                ImGui.SetNextWindowPos(new Vector2(15, 15), SetCondition.Always);
                ImGui.SetNextWindowSize(new Vector2(width - 30, height - 30), SetCondition.Always);
                ImGui.PushStyleVar(StyleVar.WindowRounding, 0.1f);
                ImGui.BeginWindow("Debug Console", WindowFlags.NoResize | WindowFlags.NoCollapse | WindowFlags.NoMove);
                {
                    Vector2 size;
                    size.X = ImGui.GetWindowWidth() - 20;
                    size.Y = ImGui.GetWindowHeight() - 60;

                    bool scrollToBottom = false;
                    if (ImGui.BeginChild("ConsoleEntries", size, true, WindowFlags.NoResize))
                    {
                        if (_previousFrameLines < _lines.Count)
                        {
                            scrollToBottom = true;
                        }

                        _previousFrameLines = _lines.Count;
                        foreach (var entry in _lines)
                        {
                            ImGui.Text(entry);
                        }

                        if (scrollToBottom)
                        {
                            ImGuiNative.igSetScrollHere();
                        }

                        ImGui.EndChild();
                    }

                    if (_focusInput)
                    {
                        ImGuiNative.igSetKeyboardFocusHere(0);
                        _focusInput = false;
                    }
                    if (ImGui.InputText(
                        string.Empty,
                        _inputBuffer.Buffer,
                        _inputBuffer.Length,
                        InputTextFlags.AutoSelectAll | InputTextFlags.EnterReturnsTrue,
                        null))
                    {
                        SubmitCommand(_inputBuffer.GetString());
                    }
                }

                ImGui.EndWindow();
                ImGui.PopStyleVar();
            }
        }

        private void SubmitCommand(string command)
        {
            string[] args = command.Split(' ');
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo(args[0], string.Join(" ", args.Skip(1)))
                {
                    RedirectStandardOutput = true
                }
            };

            try
            {
                p.Start();
                Task.Run(() =>
                {

                    while (!p.StandardOutput.EndOfStream)
                    {
                        string output = p.StandardOutput.ReadLine();
                        AddLine(output);
                    }
                });
            }
            catch (Exception e)
            {
                AddLine("Error starting process");
                AddLine(e.ToString());
            }

            _inputBuffer.ClearData();
            _focusInput = true;
        }

        private void AddLine(string text)
        {
            _lines = _lines.Add(text);
        }
    }
}
