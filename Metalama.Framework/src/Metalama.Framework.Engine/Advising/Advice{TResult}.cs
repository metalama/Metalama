// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Advising;

internal abstract class Advice<TResult> : Advice
    where TResult : AdviceResult
{
    protected Advice( in AdviceConstructorParameters parameters ) : base( parameters ) { }

    public TResult Execute( IAdviceExecutionContext context )
    {
        // Initialize the advice. It should report errors for any situation that does not depend on the target declaration.
        // These errors are reported as exceptions.
        var initializationDiagnostics = new DiagnosticBag();
        var implementationContext = new AdviceImplementationContext( initializationDiagnostics, context );
        var adviceResult = this.Implement( implementationContext );
        implementationContext.ThrowIfAnyError();

        context.Diagnostics.Report( initializationDiagnostics );
        context.Diagnostics.Report( adviceResult.ReportedDiagnostics );

        context.IntrospectionListener?.AddAdviceResult( this.AspectInstance, this, adviceResult, context.MutableCompilation );

        // Process outcome.
        switch ( adviceResult.Outcome )
        {
            case AdviceOutcome.Error:
                context.AspectInstance.Skip();

                break;

            case AdviceOutcome.Ignore:
                break;

            default:
                var transformations = implementationContext.Transformations;
                var transitiveAspects = implementationContext.TransitiveAspects;

                context.AddTransformations( transformations );
                context.AddTransitiveAspects( transitiveAspects );

                if ( context.IntrospectionListener != null )
                {
                    adviceResult.Transformations = transformations;
                }

                break;
        }

        return adviceResult;
    }

    /// <summary>
    /// Initializes and validates the advice. Executed before any advices are executed.
    /// </summary>
    /// <remarks>
    /// The advice should only report diagnostics that do not take into account the target declaration(s).
    /// </remarks>
    protected abstract TResult Implement( AdviceImplementationContext context );

    protected TResult CreateFailedResult( Diagnostic diagnostic ) => this.CreateFailedResult( ImmutableArray.Create( diagnostic ) );

    protected abstract TResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics );
}