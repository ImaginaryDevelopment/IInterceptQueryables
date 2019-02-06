using System;

namespace QueryInterception
{
    public class Disposable : IDisposable
    {
        private readonly Action _onDispose;

        private bool _isDisposed;

        public Disposable(Action onDispose)
        {
            if (onDispose == null)
            {
                throw new ArgumentNullException("onDispose");
            }
            this._onDispose = onDispose;
        }

        public void Dispose()
        {
            if (!this._isDisposed)
            {
                this._isDisposed = true;
                this._onDispose();
            }
        }
    }
}