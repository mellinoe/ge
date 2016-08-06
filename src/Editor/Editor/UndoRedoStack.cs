using Engine.Editor.Commands;
using System.Collections.Generic;

namespace Engine.Editor
{
    public class UndoRedoStack
    {
        public int MaxHistory { get; set; } = 100;

        private LinkedList<Command> _commandStack;
        private LinkedListNode<Command> _current;
        private LinkedListNode<Command> _rootNode;

        public UndoRedoStack()
        {
            _commandStack = new LinkedList<Command>();
            _rootNode = new LinkedListNode<Command>(null);
            _commandStack.AddFirst(_rootNode);
            _current = _rootNode;
        }

        public void CommitCommand(Command c)
        {
            c.Execute();

            RemoveNodesAfter(_current);
            _current = _commandStack.AddAfter(_current, c);
            if (_commandStack.Count > MaxHistory)
            {
                _commandStack.RemoveFirst();
            }
        }

        public void UndoLatest()
        {
            if (_current != _rootNode)
            {
                _current.Value.Undo();
                _current = _current.Previous;
            }
        }

        public void RedoLatest()
        {
            if (_current != null && _current.Next != null)
            {
                _current.Next.Value.Execute();
                _current = _current.Next;
            }
        }

        private void RemoveNodesAfter(LinkedListNode<Command> node)
        {
            while (node.Next != null)
            {
                _commandStack.Remove(node.Next);
            }
        }
    }
}
