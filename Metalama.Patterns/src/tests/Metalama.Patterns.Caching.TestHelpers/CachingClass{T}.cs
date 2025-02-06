// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.TestHelpers
{
    public partial class CachingClass<T>
        where T : CachedValueClass, new()
    {
        private int _counter;

        private bool _methodCalled;

        // ReSharper disable once EventNeverSubscribedTo.Global
        public event EventHandler<T>? MethodCalled;

        public bool Reset()
        {
            var result = this._methodCalled;
            this._methodCalled = false;

            return result;
        }

        private T CreateNextValue()
        {
            if ( this._methodCalled )
            {
                throw new InvalidOperationException(
                    "Cached method called twice unexpectedly. If this is the expected behavior, call reset before the second call of the method." );
            }

            this._methodCalled = true;

            var value = new T() { Id = this._counter++ };
            this.MethodCalled?.Invoke( this, value );

            return value;
        }

        private async Task<T> CreateNextValueAsync()
        {
            await Task.Delay( 1 );

            return this.CreateNextValue();
        }

        private T CreateNextValueAsDependency()
        {
            var value = this.CreateNextValue();
            CachingService.Default.AddObjectDependency( value );

            return value;
        }

        private async Task<T> CreateNextValueAsDependencyAsync()
        {
            var value = await this.CreateNextValueAsync();
            CachingService.Default.AddObjectDependency( value );

            return value;
        }

        public virtual T GetValue()
        {
            return this.CreateNextValue();
        }

        public virtual async Task<T> GetValueAsync()
        {
            return await this.CreateNextValueAsync();
        }

        public virtual T GetValueAsDependency()
        {
            return this.CreateNextValueAsDependency();
        }

        public virtual async Task<T> GetValueAsDependencyAsync()
        {
            return await this.CreateNextValueAsDependencyAsync();
        }

        // ReSharper disable once VirtualMemberNeverOverridden.Global
        // ReSharper disable once UnusedMember.Global
        public virtual IEnumerable<T> GetValues()
        {
            yield return this.CreateNextValue();
        }
    }
}