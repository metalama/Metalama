// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Extensions.DependencyInjection;

[CompileTime]
public sealed class IntroduceDependencyResult
{
    private readonly IFieldOrProperty? _declaration;

    public AdviceOutcome Outcome { get; }

    public IFieldOrProperty Declaration
        => this._declaration ?? throw new InvalidOperationException( $"Cannot get the declaration when the outcome is {this.Outcome}." );

    private IntroduceDependencyResult( AdviceOutcome outcome, IFieldOrProperty? declaration = null )
    {
        if ( outcome is AdviceOutcome.Default or AdviceOutcome.Ignore && declaration == null )
        {
            throw new ArgumentNullException( nameof(declaration) );
        }

        this.Outcome = outcome;
        this._declaration = declaration;
    }

    public static IntroduceDependencyResult Ignore( IFieldOrProperty fieldOrProperty ) => new( AdviceOutcome.Ignore, fieldOrProperty );

    public static IntroduceDependencyResult Success( IFieldOrProperty fieldOrProperty ) => new( AdviceOutcome.Default, fieldOrProperty );

    public static IntroduceDependencyResult Error => new( AdviceOutcome.Error, null );
}