using Ge.Behaviors;
using ImGuiNET;
using System.Numerics;
using System;
using Ge.Graphics;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Reflection;

namespace Ge.Editor
{
    public class DebugConsole : Behavior
    {
        private GraphicsSystem _gs;
        private ConsoleCommandSystem _ccs;

        private ImmutableList<string> _lines = ImmutableList<string>.Empty;
        private TextInputBuffer _inputBuffer = new TextInputBuffer(1024);
        private bool _windowOpen;
        private int _previousFrameLines;
        private bool _focusInput;

        protected override void Start(SystemRegistry registry)
        {
            _ccs = registry.GetSystem<ConsoleCommandSystem>();
            _ccs.Print += AddLine;
            _gs = registry.GetSystem<GraphicsSystem>();

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
                        _ccs.SubmitCommand(_inputBuffer.GetString());
                        _inputBuffer.ClearData();
                        _focusInput = true;
                    }
                }

                ImGui.EndWindow();
                ImGui.PopStyleVar();
            }
        }

        public void AddLine(string text)
        {
            _lines = _lines.Add(text);
        }
    }

    public class ConsoleCommandSystem : GameSystem
    {
        private readonly List<ConsoleCommandOption> _commandOptions = new List<ConsoleCommandOption>();
        private readonly SystemRegistry _registry;

        public event Action<string> Print;

        public ConsoleCommandSystem(SystemRegistry registry)
        {
            _registry = registry;
            LoadCommands(typeof(ConsoleCommandSystem).GetTypeInfo().Assembly);
        }

        public override void Update(float deltaSeconds)
        {
        }

        public void SubmitCommand(string command)
        {
            string[] args = command.Split(' ');
            string name = args[0];

            Type cmdType = _commandOptions.FirstOrDefault(cco => cco.Names.Contains(name)).CommandType;
            if (cmdType == null)
            {
                AddLine("Invalid command name: " + name);
            }
            else
            {
                ConsoleCommand cmd = (ConsoleCommand)Activator.CreateInstance(cmdType);
                cmd.Log += AddLine;
                cmd.Execute(string.Join(" ", args.Skip(1)), _registry);
            }
        }

        private void AddLine(string message)
        {
            Print?.Invoke(message);
        }

        private void LoadCommands(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(t =>
            {
                Type ccType = typeof(ConsoleCommand);
                return t != ccType && t != typeof(HelpCommand)
                    && ccType.IsAssignableFrom(t);
            });
            _commandOptions.AddRange(types.Select(t => new ConsoleCommandOption((ConsoleCommand)Activator.CreateInstance(t))));
            _commandOptions.Add(new ConsoleCommandOption(new HelpCommand()));
        }

        private class HelpCommand : ConsoleCommand
        {

            public override string[] Aliases { get; } = { "-?", "-h" };
            public override string Name => "help";

            public HelpCommand()
            {
            }

            public override void Execute(string args, SystemRegistry registry)
            {
                var options = registry.GetSystem<ConsoleCommandSystem>()._commandOptions;
                Print("Availiable commands:");
                foreach (var option in options)
                {
                    string name = option.Names.Last();
                    var aliases = option.Names.Take(option.Names.Length - 1);
                    Print($"    {name}, ({string.Join(", ", aliases)})");
                }
            }
        }

        private struct ConsoleCommandOption
        {
            public string[] Names { get; }
            public Type CommandType { get; }

            public ConsoleCommandOption(ConsoleCommand cmd)
            {
                Names = cmd.Aliases.Append(cmd.Name).ToArray();
                CommandType = cmd.GetType();
            }
        }
    }
}
