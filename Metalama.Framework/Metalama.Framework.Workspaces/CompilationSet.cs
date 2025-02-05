// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Workspaces;

internal sealed class CompilationSet : ICompilationSet
{
    private readonly string _name;

    public CompilationSet( string name, ImmutableArray<ICompilation> compilations )
    {
        this._name = name;
        this.Compilations = compilations;
    }

    public ImmutableArray<ICompilation> Compilations { get; }

    [Memo]
    public ImmutableArray<INamedType> Types => this.Compilations.AsParallel().SelectMany( p => p.Types ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IMethod> Methods => this.Types.AsParallel().SelectMany( t => t.Methods ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IField> Fields => this.Types.AsParallel().SelectMany( t => t.Fields ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IProperty> Properties => this.Types.AsParallel().SelectMany( t => t.Properties ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IFieldOrProperty> FieldsAndProperties => this.Types.AsParallel().SelectMany( t => t.FieldsAndProperties ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IConstructor> Constructors => this.Types.AsParallel().SelectMany( t => t.Constructors ).ToImmutableArray();

    [Memo]
    public ImmutableArray<IEvent> Events => this.Types.AsParallel().SelectMany( t => t.Events ).ToImmutableArray();

    [Memo]
    public ImmutableArray<string> TargetFrameworks
        => this.Compilations.Select( p => p.Project.TargetFramework )
            .WhereNotNull()
            .Distinct()
            .OrderBy( s => s.ToUpperInvariant() )
            .ToImmutableArray();

    public override string ToString() => this._name;
}