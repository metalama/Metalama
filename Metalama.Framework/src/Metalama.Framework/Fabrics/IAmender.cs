// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Project;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Fabrics
{
    [InternalImplement]
    [CompileTime]
    public interface IAmender
    {
        /// <summary>
        /// Gets the project being built.
        /// </summary>
        IProject Project { get; }
    }

    /// <summary>
    /// Base interface for the argument of <see cref="ProjectFabric.AmendProject"/>, <see cref="NamespaceFabric.AmendNamespace"/>
    /// or <see cref="TypeFabric.AmendType"/>. Allows to report diagnostics and add aspects to the target declaration of the fabric.
    /// </summary>
    public interface IAmender<out T> : IAmender, IQuery<T>
        where T : class, IDeclaration
    {
        new IProject Project { get; }

        /// <summary>
        /// Gets an object that allows to add child advice and to validate code and code references.
        /// </summary>
        [Obsolete( "The Outbound interface is now directly implemented by IAmender<T>." )]
        IQuery<T> Outbound { get; }
    }
}