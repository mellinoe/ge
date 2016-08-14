using System;

namespace Engine.Editor.Commands
{
    public class RawCommand : Command
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;

        public RawCommand(Action execute, Action undo)
        {
            _executeAction = execute;
            _undoAction = undo;
        }

        public override void Execute()
        {
            _executeAction();
        }

        public override void Undo()
        {
            _undoAction();
        }
    }
}
