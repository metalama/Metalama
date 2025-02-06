// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Text;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

/// <summary>
/// Represents a named type in an observability dependency graph.
/// </summary>
[CompileTime]
internal class ObservableTypeInfo
{
    private readonly Dictionary<IFieldOrProperty, ObservablePropertyInfo> _properties = new();
    private readonly HashSet<ObservableExpression> _allExpressions = new();

    public DependencyGraphBuilder Builder { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public INamedType Type { get; }

    public ObservableTypeInfo( DependencyGraphBuilder builder, INamedType type )
    {
        this.Builder = builder;
        this.Type = type;
    }

    public override string ToString() => this.ToString( null );

    public string ToString( ObservableExpression? highlightNode )
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine( "<root>" );

        foreach ( var member in this._properties.Values.OrderBy( x => x.Name ) )
        {
            member.RootReferenceNode.ToString( stringBuilder, 2, x => x == highlightNode );
        }

        return stringBuilder.ToString();
    }

    public ObservablePropertyInfo GetOrAddProperty( IFieldOrProperty fieldOrProperty )
    {
        if ( !this._properties.TryGetValue( fieldOrProperty, out var member ) )
        {
            member = this.Builder.CreatePropertyInfo( fieldOrProperty, this );
            this._properties.Add( fieldOrProperty, member );
        }

        return member;
    }

    internal void AddExpression( ObservableExpression reference )
    {
        if ( this._allExpressions.Add( reference ) && reference.Parent != null )
        {
            this.AddExpression( reference.Parent );
        }
    }

    protected IEnumerable<ObservablePropertyInfo> Properties => this._properties.Values;

    protected IReadOnlyCollection<ObservableExpression> AllExpressions => this._allExpressions;
}