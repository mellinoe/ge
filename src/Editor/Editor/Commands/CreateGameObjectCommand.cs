using System;

namespace Engine.Editor.Commands
{
    public class CreateGameObjectCommand : Command
    {
        private GameObject _go;

        public string Name { get; }
        public Transform Parent { get; }
        public GameObject GameObject => _go;

        public CreateGameObjectCommand(string name, Transform parent = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Parent = parent;
        }

        public override void Execute()
        {
            _go = new GameObject(Name);
            if (Parent != null)
            {
                _go.Transform.Parent = Parent;
            }
        }

        public override void Undo()
        {
            _go.Destroy();
        }
    }
}
