// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Services
{
    /// <summary>
    /// A non-generic base class for <see cref="ServiceProvider{TBase}"/>.
    /// </summary>
    [PublicAPI]
    public abstract class ServiceProvider : IDisposable
    {
        internal IServiceProvider? NextProvider { get; private protected set; }

        internal ServiceProvider<T>? FindNext<T>()
            where T : class
        {
            for ( var i = this.NextProvider as ServiceProvider; i != null; i = i.NextProvider as ServiceProvider )
            {
                if ( i is ServiceProvider<T> good )
                {
                    return good;
                }
            }

            return null;
        }

        public virtual void Dispose() { }
    }
}