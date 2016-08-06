namespace Engine.Editor.Commands
{
    public class CompoundCommand : Command
    {
        private readonly Command[] _children;

        public CompoundCommand(params Command[] commands)
        {
            _children = commands;
        }

        public override void Execute()
        {
            for (int i = 0; i < _children.Length; i++)
            {
                Command child = _children[i];
                child.Execute();
            }
        }

        public override void Undo()
        {
            // Undo in reverse order
            for (int i = _children.Length - 1; i >= 0; i--)
            {
                Command child = _children[i];
                child.Undo();
            }
        }
    }
}
