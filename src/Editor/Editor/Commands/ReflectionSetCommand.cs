using System.Reflection;

namespace Engine.Editor.Commands
{
    public class ReflectionSetCommand : Command
    {
        private readonly ReflectionSettable _settable;
        private readonly object _targetObject;
        private readonly object _originalValue;
        private readonly object _newValue;

        public ReflectionSetCommand(ReflectionSettable settable, object target, object originalValue, object newValue)
        {
            _settable = settable;
            _targetObject = target;
            _originalValue = originalValue;
            _newValue = newValue;
        }

        public override void Execute()
        {
            _settable.SetValue(_targetObject, _newValue);
        }

        public override void Undo()
        {
            _settable.SetValue(_targetObject, _originalValue);
        }
    }

    public abstract class ReflectionSettable
    {
        public abstract void SetValue(object obj, object value);
    }

    public class PropertySettable : ReflectionSettable
    {
        private readonly PropertyInfo _property;

        public PropertySettable(PropertyInfo property)
        {
            _property = property;
        }

        public override void SetValue(object obj, object value)
        {
            _property.SetValue(obj, value);
        }
    }

    public class FieldSettable : ReflectionSettable
    {
        private readonly FieldInfo _field;

        public FieldSettable(FieldInfo property)
        {
            _field = property;
        }

        public override void SetValue(object obj, object value)
        {
            _field.SetValue(obj, value);
        }
    }
}
