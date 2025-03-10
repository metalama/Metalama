// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Fabrics;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries.Aspects;

internal sealed class AspectQuerySource<TDeclaration> : IAspectSource
    where TDeclaration : class, IDeclaration
{
    private readonly IQueryImpl<TDeclaration> _query;
    private readonly Func<TDeclaration, object?, QueryExecutionContext, AspectInstanceCollector, Task> _addResult;
    private readonly EligibleScenarios? _eligibleScenarios;

    public ImmutableArray<IAspectClass> AspectClasses { get; }

    public AspectQuerySource(
        IAspectClass aspectClass,
        IQueryImpl<TDeclaration> query,
        Func<TDeclaration, object?, QueryExecutionContext, AspectInstanceCollector, Task> addResult,
        EligibleScenarios? eligibleScenarios )
    {
        this._query = query;
        this._addResult = addResult;
        this._eligibleScenarios = eligibleScenarios;
        this.AspectClasses = ImmutableArray.Create( aspectClass );
    }

    public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
    {
        return this._query.InvokeAsync(
            collector.Compilation,
            collector.Diagnostics,
            GeneralDiagnosticDescriptors.CanAddChildAspectOnlyUnderParent,
            ProcessTarget,
            collector.CancellationToken );

        Task ProcessTarget( TDeclaration targetDeclaration, object? tag, QueryExecutionContext queryExecutionContext )
        {
            queryExecutionContext.CancellationToken.ThrowIfCancellationRequested();

            if ( targetDeclaration == null! )
            {
                return Task.CompletedTask;
            }

            var predecessorInstance = (IAspectPredecessorImpl) this._query.Owner.AspectPredecessor.Instance;
            
            // Verify eligibility.
            var aspectClass = (AspectClass) collector.AspectClass;
            var eligibility = aspectClass.GetEligibility( targetDeclaration );

            if ( this._eligibleScenarios != null && !eligibility.IncludesAny( this._eligibleScenarios.Value ) )
            {
                // Ineligible scenarios must be silently ignored when _eligibleScenarios is non-null.
                return Task.CompletedTask;
            }

            var canBeInherited = targetDeclaration.CanBeInherited();
            var requiredEligibility = canBeInherited ? EligibleScenarios.Default | EligibleScenarios.Inheritance : EligibleScenarios.Default;

            if ( !eligibility.IncludesAny( requiredEligibility ) )
            {
                var reason = aspectClass.GetIneligibilityJustification(
                    requiredEligibility,
                    new DescribedObject<IDeclaration>( targetDeclaration ) )!;

                queryExecutionContext.DiagnosticSink.Report(
                    GeneralDiagnosticDescriptors.IneligibleChildAspect.CreateRoslynDiagnostic(
                        predecessorInstance.GetDiagnosticLocation( collector.Compilation.RoslynCompilation ),
                        (predecessorInstance.FormatPredecessor( collector.Compilation ), aspectClass.ShortName, targetDeclaration, reason) ) );

                return Task.CompletedTask;
            }

            return queryExecutionContext.UserCodeInvoker.InvokeAsync(
                () => this._addResult( targetDeclaration, tag, queryExecutionContext, collector ),
                queryExecutionContext.UserCodeExecutionContext );
        }
    }
}