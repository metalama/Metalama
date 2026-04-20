// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Represents the result of introducing a dependency into a type.
/// </summary>
/// <seealso cref="DependencyInjectionExtensions.IntroduceDependency(IAdviser{INamedType}, IType, DependencyOptions)"/>
/// <seealso href="@dependency-injection"/>
[CompileTime]
[PublicAPI]
public sealed class IntroduceDependencyResult
{
    // ReSharper disable once ReplaceWithFieldKeyword
    private readonly IFieldOrProperty? _declaration;

    /// <summary>
    /// Gets the outcome of the advice operation.
    /// </summary>
    public AdviceOutcome Outcome { get; }

    /// <summary>
    /// Gets the field or property that was introduced or found to represent the dependency.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Outcome"/> indicates failure.</exception>
    public IFieldOrProperty Declaration
        => this._declaration ?? throw new InvalidOperationException( $"Cannot get the declaration when the outcome is {this.Outcome}." );

    private IntroduceDependencyResult( AdviceOutcome outcome, IFieldOrProperty? declaration = null )
    {
        if ( outcome is AdviceOutcome.Default or AdviceOutcome.Ignore && declaration == null )
        {
            throw new ArgumentNullException( nameof(declaration) );
        }

        this.Outcome = outcome;
        this._declaration = declaration!;
    }

    /// <summary>
    /// Creates a result indicating that the dependency introduction was ignored because a suitable member already exists.
    /// </summary>
    /// <param name="fieldOrProperty">The existing field or property.</param>
    /// <returns>An <see cref="IntroduceDependencyResult"/> with outcome <see cref="AdviceOutcome.Ignore"/>.</returns>
    public static IntroduceDependencyResult Ignore( IFieldOrProperty fieldOrProperty ) => new( AdviceOutcome.Ignore, fieldOrProperty );

    /// <summary>
    /// Creates a result indicating successful introduction of a dependency.
    /// </summary>
    /// <param name="fieldOrProperty">The introduced or found field or property.</param>
    /// <returns>An <see cref="IntroduceDependencyResult"/> with outcome <see cref="AdviceOutcome.Default"/>.</returns>
    public static IntroduceDependencyResult Success( IFieldOrProperty fieldOrProperty ) => new( AdviceOutcome.Default, fieldOrProperty );

    /// <summary>
    /// Gets a result indicating that the dependency introduction failed.
    /// </summary>
    public static IntroduceDependencyResult Error => new( AdviceOutcome.Error );
}