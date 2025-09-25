using System;

namespace ET
{
    public interface ISingletonReverseDispose
    {
        
    }
    
    public abstract class ASingleton : IDisposable
    {
        internal abstract void Register();
        public virtual void Dispose()
        {
            
        }
    }
    
    public abstract class Singleton<T>: ASingleton where T: Singleton<T>
    {
        private bool isDisposed;
        
        [StaticField]
        private static T instance;
        
        [StaticField]
        public static T Instance
        {
            get
            {
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        internal override void Register()
        {
            Instance = (T)this;
        }

        public bool IsDisposed()
        {
            return this.isDisposed;
        }

        protected virtual void Destroy()
        {
            
        }

        public override void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }
            
            this.isDisposed = true;

            this.Destroy();
            base.Dispose();
            Instance = null;
        }
    }
}