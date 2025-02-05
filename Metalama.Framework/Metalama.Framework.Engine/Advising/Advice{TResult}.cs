// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Advising;

internal abstract class Advice<TResult> : Advice
    where TResult : AdviceResult, new()
{
    protected Advice( AdviceConstructorParameters parameters ) : base( parameters ) { }

    public TResult Execute( IAdviceExecutionContext context )
    {
        List<ITransformation> transformations = new();

        // Initialize the advice. It should report errors for any situation that does not depend on the target declaration.
        // These errors are reported as exceptions.
        var initializationDiagnostics = new DiagnosticBag();
        var implementationContext = new AdviceImplementationContext( initializationDiagnostics, context, transformations );
        var adviceResult = this.Implement( implementationContext );
        implementationContext.ThrowIfAnyError();

        context.Diagnostics.Report( initializationDiagnostics );
        context.Diagnostics.Report( adviceResult.ReportedDiagnostics );

        // Set the compilation in which references must be resolved.
        adviceResult.Compilation = context.MutableCompilation;

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
                context.AddTransformations( transformations );

                if ( context.IntrospectionListener != null )
                {
                    adviceResult.Transformations = transformations.ToImmutableArray();
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
    protected abstract TResult Implement( in AdviceImplementationContext context );

    protected TResult CreateFailedResult( Diagnostic diagnostic )
        => new() { ReportedDiagnostics = ImmutableArray.Create( diagnostic ), Outcome = AdviceOutcome.Error, AdviceKind = this.AdviceKind };

    protected TResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new() { ReportedDiagnostics = diagnostics, Outcome = AdviceOutcome.Error, AdviceKind = this.AdviceKind };

    internal readonly struct AdviceImplementationContext
    {
        private readonly List<ITransformation> _transformations;

        private readonly IAdviceExecutionContext _adviceExecutionContext;

        public AdviceImplementationContext( DiagnosticBag diagnostics, IAdviceExecutionContext adviceExecutionContext, List<ITransformation> transformations )
        {
            this._transformations = transformations;
            this.Diagnostics = diagnostics;
            this._adviceExecutionContext = adviceExecutionContext;
        }

        public void ThrowIfAnyError()
        {
            if ( this.Diagnostics.HasError() )
            {
                throw new DiagnosticException(
                    "Errors have occured while creating advice.",
                    this.Diagnostics.Where( d => d.Severity == DiagnosticSeverity.Error ).ToImmutableArray() );
            }
        }

        public CompilationModel MutableCompilation => this._adviceExecutionContext.MutableCompilation;

        public ProjectServiceProvider ServiceProvider => this._adviceExecutionContext.ServiceProvider;

        public DiagnosticBag Diagnostics { get; }

        public void AddTransformation( ITransformation transformation )
        {
            this._adviceExecutionContext.SetOrders( transformation );
            this._transformations.Add( transformation );
        }
    }
}