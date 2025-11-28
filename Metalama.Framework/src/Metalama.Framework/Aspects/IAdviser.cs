// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Aspects;

/// <summary>
/// An object that allows declarations to be advised (transformed) using extension methods from <see cref="AdviserExtensions"/>.
/// This is the non-generic base interface; aspects typically use the generic <see cref="IAdviser{T}"/> variant.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IAdviser"/> interface provides the foundation for code transformation in Metalama. It exposes:
/// </para>
/// <list type="bullet">
/// <item><description><b>Target declaration:</b> The code element being advised (method, type, property, etc.)</description></item>
/// <item><description><b>Diagnostics:</b> Report errors, warnings, or suppress compiler diagnostics</description></item>
/// <item><description><b>Compilation context:</b> Access to the code model in both original and modified states</description></item>
/// <item><description><b>Declaration switching:</b> Create adviser instances for other declarations via <see cref="With{TNewDeclaration}"/></description></item>
/// </list>
/// <para>
/// Advising is performed through extension methods in <see cref="AdviserExtensions"/>, such as <c>Override()</c>,
/// <c>IntroduceMethod()</c>, <c>ImplementInterface()</c>, and others.
/// </para>
/// </remarks>
/// <seealso cref="IAdviser{T}"/>
/// <seealso cref="AdviserExtensions"/>
/// <seealso cref="IAspectBuilder"/>
/// <seealso href="@advising-code"/>
/// <seealso href="@aspect-design"/>
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
    /// Gets the compilation in its original state, before any modifications by aspects.
    /// </summary>
    ICompilation Compilation { get; }

    /// <summary>
    /// Gets the mutable compilation that the current <see cref="IAspectBuilder"/> is working on. It includes all modifications made by
    /// the current aspect so far, including advice added programmatically via <see cref="AdviserExtensions"/> methods and
    /// advice added declaratively via attributes like <see cref="IntroduceAttribute"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="MutableCompilation"/> when you need to query the code model with transformations from the current aspect applied.
    /// Use <see cref="Compilation"/> when you need to see the original, unmodified code model.
    /// </para>
    /// </remarks>
    /// <seealso cref="Compilation"/>
    ICompilation MutableCompilation { get; }

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