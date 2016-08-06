using System;

namespace Engine.Editor.Commands
{
    public class SetValueActionCommand : Command
    {
        private readonly Action<object> _setValueAction;
        private readonly object _originalValue;
        private readonly object _newValue;

        public SetValueActionCommand(Action<object> setValueAction, object originalValue, object newValue)
        {
            _setValueAction = setValueAction;
            _originalValue = originalValue;
            _newValue = newValue;
        }

        public static SetValueActionCommand New<T>(Action<T> setValueAction, object originalValue, object newValue)
        {
            return new SetValueActionCommand((o) => setValueAction((T)o), originalValue, newValue);
        }

        public override void Execute()
        {
            _setValueAction(_newValue);
        }

        public override void Undo()
        {
            _setValueAction(_originalValue);
        }
    }
}
