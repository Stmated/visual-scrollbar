#region

using System;

#endregion

namespace Eliason.Scrollbar
{
    internal class EventArgs<T> : EventArgs
    {
        private readonly T data;

        public EventArgs(T data)
        {
            this.data = data;
        }

        public T Data
        {
            get { return this.data; }
        }
    }
}