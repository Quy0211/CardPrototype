using System;
using UnityEngine.UIElements;

namespace CardGame.Core
{
    /// <summary>
    /// Base cho các model có thể bind. Unity 6 phát sự kiện qua
    /// EventHandler&lt;BindablePropertyChangedEventArgs&gt; (không phải PropertyChangedEventArgs).
    /// </summary>
    public abstract class BindableBase : INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        protected void Raise(string propertyName)
        {
            var handler = propertyChanged;
            if (handler == null) return;
            BindingId id = propertyName;
            handler(this, new BindablePropertyChangedEventArgs(in id));
        }
    }
}