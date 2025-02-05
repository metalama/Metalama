// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Workspaces;

internal sealed class CompilationSetResult : ICompilationSetResult
{
    private readonly string _name;

    private ImmutableArray<IIntrospectionCompilationResult> CompilationResults { get; }

    public CompilationSetResult( ImmutableArray<IIntrospectionCompilationResult> compilationsResults, string name )
    {
        this._name = name;
        this.CompilationResults = compilationsResults;
    }

    [Memo]
    public ICompilationSet TransformedCode => new CompilationSet( this._name, this.AggregateResults( r => new[] { r.TransformedCode } ).ToImmutableArray() );

    [Memo]
    public ImmutableArray<IIntrospectionAspectLayer> AspectLayers
        => this.AggregateResults( c => c.AspectLayers )
            .GroupBy( l => l.Id )
            .Select( l => IntrospectionMapper.AggregateAspectLayers( this.AspectClasses.Single( c => c.FullName == l.First().AspectClass.FullName ), l ) )
            .ToImmutableArray();

    [Memo]
    public ImmutableArray<IIntrospectionAspectInstance> AspectInstances => this.AggregateResults( x => x.AspectInstances ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IIntrospectionAspectClass> AspectClasses
        => this.AggregateResults( c => c.AspectClasses )
            .GroupBy( x => x.FullName )
            .Select( group => IntrospectionMapper.AggregateAspectClasses( group.First(), group.SelectMany( x => x.Instances ) ) )
            .ToImmutableArray();

    [Memo]
    public ImmutableArray<IIntrospectionDiagnostic> Diagnostics => this.AggregateResults( x => x.Diagnostics ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IIntrospectionAdvice> Advice => this.AggregateResults( x => x.Advice ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IIntrospectionTransformation> Transformations => this.AggregateResults( x => x.Transformations ).ToImmutableArray();

    [Memo]
    public bool IsMetalamaEnabled => this.AggregateResults( x => new[] { x.IsMetalamaEnabled } ).Any();

    public bool HasMetalamaSucceeded => this.AggregateResults( x => new[] { x.HasMetalamaSucceeded } ).Any();

    private List<T> AggregateResults<T>( Func<IIntrospectionCompilationResult, IEnumerable<T>> func )
    {
        var results = new List<T>();
        var failedProjects = new List<string>();
        var diagnostics = new List<IIntrospectionDiagnostic>();

        foreach ( var compilationResult in this.CompilationResults )
        {
            try
            {
                results.AddRange( func( compilationResult ) );
            }
            catch ( CompilationFailedException e )
            {
                failedProjects.Add( compilationResult.Name );
                diagnostics.AddRange( e.Diagnostics );
            }
        }

        if ( diagnostics.Count > 0 )
        {
            throw new CompilationFailedException(
                $"The compilation of project(s) {string.Join( ", ", failedProjects.SelectAsReadOnlyList( x => $"'{x}'" ) )} failed. Check the Diagnostics collection for details. Use WithIgnoreErrors(true) to ignore errors.",
                diagnostics.ToImmutableArray() );
        }

        return results;
    }
}