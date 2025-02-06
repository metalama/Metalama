// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Patterns.Observability.Implementation;
using Metalama.Patterns.Observability.Implementation.ClassicStrategy;

namespace Metalama.Patterns.Observability.Configuration;

[CompileTime]
internal sealed record ObservabilityOptions : IHierarchicalOptions<ICompilation>, IHierarchicalOptions<INamespace>, IHierarchicalOptions<INamedType>
{
    /// <summary>
    /// Gets the <see cref="IObservabilityStrategy"/> implementing the <see cref="ObservableAttribute"/> aspect.
    /// </summary>
    public IObservabilityStrategy? ImplementationStrategy { get; init; }

    private readonly int? _diagnosticCommentVerbosity;

    /// <summary>
    /// Gets a value indicating the verbosity of diagnostic comments inserted into generated code. Must be a value
    /// between 0 and 3 (inclusive). 0 (default) inserts no comments, 3 is the most verbose.
    /// </summary>
    public int? DiagnosticCommentVerbosity
    {
        get => this._diagnosticCommentVerbosity;
        init
        {
            if ( value is < 0 or > 3 )
            {
                throw new ArgumentOutOfRangeException( nameof(value) );
            }

            this._diagnosticCommentVerbosity = value;
        }
    }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (ObservabilityOptions) changes;

        return new ObservabilityOptions
        {
            ImplementationStrategy = other.ImplementationStrategy ?? this.ImplementationStrategy,
            DiagnosticCommentVerbosity = other.DiagnosticCommentVerbosity ?? this.DiagnosticCommentVerbosity
        };
    }

    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context )
    {
        const string inpcDiagnosticCommentVerbosity = "InpcDiagnosticCommentVerbosity";

        var diagnosticCommentVerbosity = 0;

        if ( context.Project.TryGetProperty( inpcDiagnosticCommentVerbosity, out var verbosityStr ) && !string.IsNullOrWhiteSpace( verbosityStr ) )
        {
            if ( !int.TryParse( verbosityStr, out diagnosticCommentVerbosity ) || diagnosticCommentVerbosity < 0 || diagnosticCommentVerbosity > 3 )
            {
                context.Diagnostics.Report(
                    DiagnosticDescriptors.WarningInvalidProjectPropertyValueWillBeIgnored.WithArguments(
                        (inpcDiagnosticCommentVerbosity,
                         verbosityStr,
                         "be an integer between 0 and 3 inclusive.") ) );
            }
        }

        return new ObservabilityOptions()
        {
            ImplementationStrategy = ClassicObservabilityStrategy.Instance, DiagnosticCommentVerbosity = diagnosticCommentVerbosity
        };
    }
}