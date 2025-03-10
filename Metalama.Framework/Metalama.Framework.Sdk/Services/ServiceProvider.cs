// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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