using System;
using ACS_Common.Base;

namespace ACS_Common.Utils
{
    public class ValueChangeDelegate<T> : ACS_Object where T : IComparable
    {
        private T _previousValue;
        private Action _onValueChangeNoParam;
        private Action<T> _onValueChange;
        public ValueChangeDelegate(Action onValueChangeCallback, T origValue)
        {
            _onValueChangeNoParam = onValueChangeCallback;
            _previousValue = origValue;
        }
        public ValueChangeDelegate(Action<T> onValueChangeCallback, T origValue)
        {
            _onValueChange = onValueChangeCallback;
            _previousValue = origValue;
        }

        public void UpdateValue(T newValue)
        {
            const string m = nameof(UpdateValue);
            // LogMethod(m, $"newValue: {newValue}, _previousValue: {_previousValue}, _onValueChangeNoParam: {_onValueChangeNoParam}");
            if (_previousValue.CompareTo(newValue) == 0) return;
            _previousValue = newValue;
            _onValueChangeNoParam?.Invoke();
            _onValueChange?.Invoke(newValue);

        }
    }
}