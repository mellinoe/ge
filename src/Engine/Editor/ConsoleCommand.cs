using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Ge.Editor
{
    public abstract class ConsoleCommand
    {
        public abstract string Name { get; }
        public abstract string[] Aliases { get; }
        public event Action<string> Log;
        public abstract void Execute(string args, SystemRegistry registry);

        protected void Print(string message) => Log?.Invoke(message);
    }

    public class ListGameObjectsCommand : ConsoleCommand
    {
        public override string Name => "ListGameObjects";
        public override string[] Aliases { get; } = { "lg" };

        public override void Execute(string args, SystemRegistry registry)
        {
            var topLevelGOs = registry.GetSystem<GameObjectQuerySystem>().GetUnparentedGameObjects();
            foreach (var go in topLevelGOs)
            {
                PrintGo(go, 0);
            }
        }

        private void PrintGo(GameObject go, int level)
        {
            string prefix = new string(' ', level * 2) + (level > 0 ? ">" : "");
            Print($"{prefix}{go.Name}");
            foreach (var child in go.Transform.Children)
            {
                PrintGo(child.GameObject, level + 1);
            }
        }
    }

    public class ExecCommand : ConsoleCommand
    {
        public override string[] Aliases => Array.Empty<string>();

        public override string Name => "exec";

        public override void Execute(string args2, SystemRegistry registry)
        {
            string[] args = args2.Split(' ');
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
                        Print(output);
                    }
                });
            }
            catch (Exception e)
            {
                Print("Error starting process");
                Print(e.ToString());
            }
        }
    }
}
