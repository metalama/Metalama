// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class ExternalAssemblyTypeCollection : INamedTypeCollection
{
    private readonly IAssemblySymbol _symbol;
    private readonly CompilationModel _compilation;
    private readonly bool _includeNestedTypes;
    private List<INamedTypeSymbol>? _types;

    public ExternalAssemblyTypeCollection( IAssemblySymbol symbol, CompilationModel compilation, bool includeNestedTypes )
    {
        this._symbol = symbol;
        this._compilation = compilation;
        this._includeNestedTypes = includeNestedTypes;
    }

    private List<INamedTypeSymbol> GetContent()
    {
        if ( this._types == null )
        {
            var topLevelTypes = this._symbol.GetTypes().Where( t => !IsHidden( t ) );

            if ( !this._includeNestedTypes )
            {
                this._types = topLevelTypes.ToMutableList();
            }
            else
            {
                var types = new List<INamedTypeSymbol>();

                void ProcessType( INamedTypeSymbol type )
                {
                    types.Add( type );

                    foreach ( var nestedType in type.GetTypeMembers() )
                    {
                        ProcessType( nestedType );
                    }
                }

                foreach ( var type in topLevelTypes )
                {
                    ProcessType( type );
                }

                this._types = types;
            }
        }

        return this._types;
    }

    private static bool IsHidden( INamedTypeSymbol type ) => type.DeclaredAccessibility == Accessibility.Private;

    public IEnumerable<INamedType> OfName( string name )
        => this.GetContent().Where( t => t.Name == name ).Select( x => this._compilation.Factory.GetNamedType( x ) );

    public IEnumerable<INamedType> OfTypeDefinition( INamedType typeDefinition )
        => this.GetContent()
            .Where(
                t => ((DeclarationEqualityComparer) this._compilation.Comparers.Default).IsConvertibleTo(
                    t,
                    typeDefinition.GetSymbol().AssertSymbolNotNull(),
                    ConversionKind.TypeDefinition ) )
            .Select( x => this._compilation.Factory.GetNamedType( x ) );

    public IEnumerator<INamedType> GetEnumerator()
    {
        foreach ( var type in this.GetContent() )
        {
            yield return this._compilation.Factory.GetNamedType( type );
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public int Count => this.GetContent().Count;
}