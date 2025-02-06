// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

/* TODO: Detect invalid constructs, including:
 * - Reference to non-INPC property (also INPC property with [IgnoreAutoChangeNotification]), unless the property has [IgnoreUnobservableExpressions].
 */

[CompileTime]
internal partial class DependencyGraphBuilder
{
    private readonly Dictionary<INamedType, ObservableTypeInfo> _types = new();

    private readonly Assets _assets;

    public DependencyGraphBuilder( Assets assets )
    {
        this._assets = assets;
    }

    protected virtual ObservableTypeInfo CreateTypeInfo( INamedType type ) => new( this, type );

    public virtual ObservablePropertyInfo CreatePropertyInfo( IFieldOrProperty fieldOrProperty, ObservableTypeInfo parent ) => new( fieldOrProperty, parent );

    public virtual ObservableExpression CreateExpression( ObservablePropertyInfo propertyInfo, ObservableExpression? parent )
        => new( propertyInfo, parent, this );

    private ObservableTypeInfo GetOrAddTypeNode( INamedType type )
    {
        if ( !this._types.TryGetValue( type, out var typeNode ) )
        {
            typeNode = this.CreateTypeInfo( type );
            this._types.Add( type, typeNode );
        }

        return typeNode;
    }

    private ObservablePropertyInfo GetOrAddPropertyNode( IFieldOrProperty property )
    {
        var declaringTypeNode = this.GetOrAddTypeNode( property.DeclaringType );

        return declaringTypeNode.GetOrAddProperty( property );
    }

    public ObservableTypeInfo GetDependencyGraph(
        INamedType type,
        GraphBuildingContext context,
        Action<string>? trace = null,
        CancellationToken cancellationToken = default )
    {
        var graphType = this.GetOrAddTypeNode( type );

        foreach ( var p in type.Properties )
        {
            if ( p.IsStatic || p.IsImplicitlyDeclared || p.Attributes.Any( this._assets.NotObservableAttribute ) )
            {
                continue;
            }

            var propertySymbol = p.GetSymbol();

            if ( propertySymbol == null )
            {
                // Only source-defined properties are supported.
                continue;
            }

            var graphMember = graphType.GetOrAddProperty( (IFieldOrProperty) type.Compilation.GetDeclaration( propertySymbol ) );

            AddReferencedProperties(
                type.Compilation,
                graphMember,
                context,
                trace,
                cancellationToken );
        }

        return graphType;
    }
}