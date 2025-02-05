// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.SerializableIds;

public abstract class SerializableTypeIdResolver<TType, TTypeOrNamespace>
    where TType : class
    where TTypeOrNamespace : class
{
    private readonly ConcurrentDictionary<SerializableTypeId, ResolverResult> _cache = new();

    // ReSharper disable once MemberCanBeInternal

    public TType ResolveId( SerializableTypeId typeId, IReadOnlyDictionary<string, TType>? genericArguments = null )
    {
        var result = this.ResolveAndCache( typeId, genericArguments! );

        if ( !result.IsSuccess )
        {
            throw new InvalidOperationException( result.ErrorMessage );
        }
        else
        {
            return result.Type;
        }
    }

    // ReSharper disable once MemberCanBeInternal

    protected abstract IReadOnlyDictionary<string, TType?> GetGenericContext( SerializableDeclarationId declarationId );

    public bool TryResolveId( SerializableTypeId typeId, [NotNullWhen( true )] out TType? type ) => this.TryResolveId( typeId, null, out type );

    // ReSharper disable once MemberCanBePrivate.Global
    public bool TryResolveId( SerializableTypeId typeId, IReadOnlyDictionary<string, TType>? genericArguments, [NotNullWhen( true )] out TType? type )
    {
        var result = this.ResolveAndCache( typeId, genericArguments! );

        if ( !result.IsSuccess )
        {
            type = null;

            return false;
        }
        else
        {
            type = result.Type;

            return true;
        }
    }

    private ResolverResult ResolveAndCache( SerializableTypeId typeId, IReadOnlyDictionary<string, TType?>? genericArguments = null )
    {
        if ( genericArguments == null || genericArguments.Count == 0 )
        {
            return this._cache.GetOrAdd( typeId, static ( id, me ) => me.ResolveCore( id ), this );
        }
        else
        {
            return this.ResolveCore( typeId, genericArguments );
        }
    }

    private ResolverResult ResolveCore( SerializableTypeId id, IReadOnlyDictionary<string, TType?>? genericArguments = null )
    {
        try
        {
            var idString = id.Id;

            TypeSyntax type;
            bool nullOblivious;

            if ( idString.StartsWith( SerializableTypeId.LegacyPrefix, StringComparison.Ordinal ) )
            {
                // Backward compatibility.

                // Extract the nullability character.
                nullOblivious = idString[^1] != '!';

                if ( !nullOblivious )
                {
                    idString = idString[..^1];
                }

                // Parse
                var expression = (TypeOfExpressionSyntax) SyntaxFactoryEx.ParseExpressionSafe( idString );
                type = expression.Type;
            }
            else
            {
                if ( idString.StartsWith( SerializableTypeId.Prefix, StringComparison.Ordinal ) )
                {
                    // TODO: optimize strings.

                    // Extract the generic context, if any.
                    var indexOfPipe = idString.IndexOfOrdinal( '|' );

                    string idWithoutContext;

                    if ( indexOfPipe > 0 )
                    {
                        idWithoutContext = idString[..indexOfPipe];
                        var contextId = idString[(indexOfPipe + 1)..];
                        genericArguments = this.GetGenericContext( new SerializableDeclarationId( contextId ) );
                    }
                    else
                    {
                        idWithoutContext = idString;
                    }

                    // Extract the nullability character.
                    nullOblivious = idWithoutContext[^1] != '!';

                    if ( !nullOblivious )
                    {
                        idWithoutContext = idWithoutContext[..^1];
                    }

                    // Parse.
                    var method = (MethodDeclarationSyntax?) SyntaxFactory.ParseMemberDeclaration(
                        idWithoutContext.Substring( SerializableTypeId.Prefix.Length ) + " M();" );

                    if ( method == null || method.GetDiagnostics().HasError() )
                    {
                        return ResolverResult.Error( $"The string '{idString}' could not be parsed as a type." );
                    }

                    type = method.ReturnType;
                }
                else
                {
                    return ResolverResult.Error( $"The string '{idString}' has an invalid prefix." );
                }
            }

            var resolver = new Resolver( this, genericArguments, nullOblivious );

            return resolver.Visit( type );
        }
        catch ( Exception e )
        {
            throw new InvalidOperationException( $"Cannot resolve the type '{id}': {e.Message}" );
        }
    }

    protected abstract TType CreateArrayType( TType elementType, int rank );

    protected abstract TType CreatePointerType( TType pointedAtType );

    protected abstract TType CreateNullableType( TType elementType );

    protected abstract TType CreateNonNullableReferenceType( TType referenceType );

    protected abstract TType ConstructGenericType( TType genericType, TType[] typeArguments );

    protected abstract TType CreateTupleType( ImmutableArray<TType> elementTypes );

    protected abstract TType DynamicType { get; }

    protected abstract TTypeOrNamespace? LookupName( string name, int arity, TTypeOrNamespace? ns );

    protected abstract TType GetSpecialType( SpecialType specialType );

    protected abstract bool HasTypeParameterOfName( TType type, string name );

    private readonly struct ResolverResult
    {
        public static ResolverResult TypeParameterOfDeclaration { get; } = new( null );

        private readonly object? _value;

        public TType Type => (TType?) this._value ?? throw new InvalidOperationException();

        public TTypeOrNamespace TypeOrNamespace => (TTypeOrNamespace?) this._value ?? throw new InvalidOperationException();

        public string? ErrorMessage => (string?) this._value;

        public bool IsSuccess => this._value is null or TType or TTypeOrNamespace;

        public bool IsTypeParameterOfDeclaration => this._value == null;

        private ResolverResult( object? value )
        {
            this._value = value;
        }

        public static ResolverResult Success( TType type ) => new( type );

        public static ResolverResult Success( TTypeOrNamespace typeOrNamespace ) => new( typeOrNamespace );

        public static ResolverResult Error( string errorMessage ) => new( errorMessage );
    }

    private sealed class Resolver : SafeSyntaxVisitor<ResolverResult>
    {
        private readonly SerializableTypeIdResolver<TType, TTypeOrNamespace> _parent;
        private readonly IReadOnlyDictionary<string, TType?>? _genericArguments;
        private readonly bool _isNullOblivious;
        private TType? _currentGenericType;

        public Resolver(
            SerializableTypeIdResolver<TType, TTypeOrNamespace> parent,
            IReadOnlyDictionary<string, TType?>? genericArguments,
            bool isNullOblivious )
        {
            this._parent = parent;
            this._genericArguments = genericArguments;
            this._isNullOblivious = isNullOblivious;
        }

        public override ResolverResult VisitArrayType( ArrayTypeSyntax node )
        {
            var elementTypeResult = this.Visit( node.ElementType );

            if ( !elementTypeResult.IsSuccess )
            {
                return elementTypeResult;
            }

            return ResolverResult.Success( this._parent.CreateArrayType( elementTypeResult.Type, node.RankSpecifiers.Count ) );
        }

        public override ResolverResult VisitPointerType( PointerTypeSyntax node )
        {
            var elementTypeResult = this.Visit( node.ElementType );

            if ( !elementTypeResult.IsSuccess )
            {
                return elementTypeResult;
            }

            return ResolverResult.Success( this._parent.CreatePointerType( elementTypeResult.Type ) );
        }

        public override ResolverResult VisitNullableType( NullableTypeSyntax node )
        {
            var elementTypeResult = this.Visit( node.ElementType );

            if ( !elementTypeResult.IsSuccess )
            {
                return elementTypeResult;
            }

            return ResolverResult.Success( this._parent.CreateNullableType( elementTypeResult.Type ) );
        }

        private ResolverResult LookupName( string name, int arity, TTypeOrNamespace? ns )
        {
            if ( name == "dynamic" )
            {
                return ResolverResult.Success( this._parent.DynamicType );
            }

            if ( ns == null )
            {
                if ( this._genericArguments != null && this._genericArguments.TryGetValue( name, out var type ) )
                {
                    return type == null
                        ? ResolverResult.Error( $"Could not resolve the type for type parameter {name}." )
                        : ResolverResult.Success( type );
                }
                else if ( this._currentGenericType != null && this._parent.HasTypeParameterOfName( this._currentGenericType, name ) )
                {
                    // We matched the name of a type parameter of the type declaration.
                    return ResolverResult.TypeParameterOfDeclaration;
                }
            }

            var member = this._parent.LookupName( name, arity, ns );

            if ( member != null )
            {
                return ResolverResult.Success( member );
            }

            return ResolverResult.Error( $"The type or namespace '{ns}' does not contain a member named '{name}' of arity {arity}." );
        }

        private ResolverResult LookupName( NameSyntax name )
        {
            var result = this.LookupName( name, null );

            if ( !result.IsSuccess )
            {
                return result;
            }
            else if ( result.IsTypeParameterOfDeclaration )
            {
                return result;
            }
            else if ( !this._isNullOblivious )
            {
                return ResolverResult.Success( this._parent.CreateNonNullableReferenceType( result.Type ) );
            }
            else
            {
                return result;
            }
        }

        private ResolverResult LookupName( NameSyntax name, TTypeOrNamespace? ns )
        {
            switch ( name )
            {
                case IdentifierNameSyntax identifierName:
                    return this.LookupName( identifierName.Identifier.Text, 0, ns );

                case QualifiedNameSyntax qualifiedName:
                    var leftResult = this.LookupName( qualifiedName.Left, ns );

                    if ( !leftResult.IsSuccess )
                    {
                        return leftResult;
                    }

                    var left = leftResult.TypeOrNamespace;

                    return this.LookupName( qualifiedName.Right, left );

                case GenericNameSyntax genericName:
                    var definitionResult = this.LookupName( genericName.Identifier.Text, genericName.Arity, ns );

                    if ( !definitionResult.IsSuccess )
                    {
                        return definitionResult;
                    }

                    var definition = definitionResult.Type;

                    if ( genericName.IsUnboundGenericName )
                    {
                        return definitionResult;
                    }
                    else
                    {
                        var previousGenericType = this._currentGenericType;
                        this._currentGenericType = definition;

                        var typeArgumentResults = genericName.TypeArgumentList.Arguments.SelectAsArray( this.Visit );

                        this._currentGenericType = previousGenericType;

                        foreach ( var typeArgumentResult in typeArgumentResults )
                        {
                            if ( !typeArgumentResult.IsSuccess )
                            {
                                return typeArgumentResult;
                            }
                            else if ( typeArgumentResult.IsTypeParameterOfDeclaration )
                            {
                                return definitionResult;
                            }
                        }

                        return ResolverResult.Success( this._parent.ConstructGenericType( definition, typeArgumentResults.SelectAsArray( r => r.Type ) ) );
                    }

                case AliasQualifiedNameSyntax aliasQualifiedName:
                    return this.LookupName( aliasQualifiedName.Name, ns );

                default:
                    throw new InvalidOperationException( $"Unexpected syntax kind: {name.Kind()}." );
            }
        }

        public override ResolverResult VisitGenericName( GenericNameSyntax node ) => this.LookupName( node );

        public override ResolverResult VisitAliasQualifiedName( AliasQualifiedNameSyntax node ) => this.LookupName( node );

        public override ResolverResult VisitQualifiedName( QualifiedNameSyntax node ) => this.LookupName( node );

        public override ResolverResult VisitIdentifierName( IdentifierNameSyntax node ) => this.LookupName( node );

        public override ResolverResult VisitTupleType( TupleTypeSyntax node )
        {
            var results = node.Elements.SelectAsMutableList( e => this.Visit( e.Type ) );

            if ( results.Any( r => !r.IsSuccess ) )
            {
                return results.FirstOrDefault( r => !r.IsSuccess );
            }

            return ResolverResult.Success( this._parent.CreateTupleType( results.SelectAsImmutableArray( x => x.Type ) ) );
        }

        public override ResolverResult DefaultVisit( SyntaxNode node ) => throw new InvalidOperationException( $"Unexpected node {node.Kind()}." );

        // Based on Microsoft.CodeAnalysis.CSharp.SyntaxKindExtensions.GetSpecialType.
        private static SpecialType GetRoslynSpecialType( SyntaxKind kind )
            => kind switch
            {
                SyntaxKind.VoidKeyword => SpecialType.System_Void,
                SyntaxKind.BoolKeyword => SpecialType.System_Boolean,
                SyntaxKind.ByteKeyword => SpecialType.System_Byte,
                SyntaxKind.SByteKeyword => SpecialType.System_SByte,
                SyntaxKind.ShortKeyword => SpecialType.System_Int16,
                SyntaxKind.UShortKeyword => SpecialType.System_UInt16,
                SyntaxKind.IntKeyword => SpecialType.System_Int32,
                SyntaxKind.UIntKeyword => SpecialType.System_UInt32,
                SyntaxKind.LongKeyword => SpecialType.System_Int64,
                SyntaxKind.ULongKeyword => SpecialType.System_UInt64,
                SyntaxKind.DoubleKeyword => SpecialType.System_Double,
                SyntaxKind.FloatKeyword => SpecialType.System_Single,
                SyntaxKind.DecimalKeyword => SpecialType.System_Decimal,
                SyntaxKind.StringKeyword => SpecialType.System_String,
                SyntaxKind.CharKeyword => SpecialType.System_Char,
                SyntaxKind.ObjectKeyword => SpecialType.System_Object,
                _ => throw new AssertionFailedException( $"Unexpected syntax kind: {kind}" )
            };

        public override ResolverResult VisitPredefinedType( PredefinedTypeSyntax node )
        {
            var specialType = GetRoslynSpecialType( node.Keyword.Kind() );

            var result = this._parent.GetSpecialType( specialType );

            if ( !this._isNullOblivious && node.Keyword.Kind() is SyntaxKind.ObjectKeyword or SyntaxKind.StringKeyword )
            {
                result = this._parent.CreateNonNullableReferenceType( result );
            }

            return ResolverResult.Success( result );
        }
    }
}