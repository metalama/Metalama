// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Advising;

/// <summary>
/// An object that allows declarations to be advised using one of the extension methods of the <see cref="AdviserExtensions"/> class.
/// This interface is the non-generic base one. All advisers implement the generic interface <see cref="IAdviser{T}"/>.
/// </summary>
[InternalImplement]
[CompileTime]
[PublicAPI]
public interface IAdviser
{
    /// <summary>
    /// Gets a service that allows to report or suppress diagnostics.
    /// </summary>
    ScopedDiagnosticSink Diagnostics { get; }

    /// <summary>
    /// Gets the declaration that will be advised.
    /// </summary>
    IDeclaration Target { get; }

    /// <summary>
    /// Gets a new <see cref="IAdviser"/> for a different <see cref="Target"/> declaration.
    /// </summary>
    IAdviser<TNewDeclaration> With<TNewDeclaration>( TNewDeclaration declaration )
        where TNewDeclaration : class, IDeclaration;
}

/// <summary>
/// An object that allows declarations to be advised using one of the extension methods of the <see cref="AdviserExtensions"/> class.
/// </summary>
[PublicAPI]
public interface IAdviser<out T> : IAdviser
{
    /// <summary>
    /// Gets the declaration that will be advised.
    /// </summary>
    new T Target { get; }
}