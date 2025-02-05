// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;
using System;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    public static SerializableDeclarationId GetSerializableId( this ISymbol symbol ) => symbol.GetSerializableId( RefTargetKind.Default );

    internal static SerializableDeclarationId GetSerializableId( this ISymbol symbol, RefTargetKind targetKind )
    {
        if ( !TryGetSerializableId( symbol, targetKind, out var id ) )
        {
            throw new ArgumentException( $"Cannot create a SerializableDeclarationId for '{symbol}'.", nameof(symbol) );
        }

        return id;
    }

    public static bool TryGetSerializableId( this ISymbol? symbol, out SerializableDeclarationId id )
        => TryGetSerializableId( symbol, RefTargetKind.Default, out id );

    private static bool TryGetSerializableId( this ISymbol? symbol, RefTargetKind targetKind, out SerializableDeclarationId id )
    {
        switch ( symbol )
        {
            case null:
            case ILocalSymbol:
            case IMethodSymbol
            {
                MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction
            }:

                id = default;

                return false;

            case IParameterSymbol parameterSymbol:
                {
                    var parentId = DocumentationCommentId.CreateDeclarationId( parameterSymbol.ContainingSymbol ).AssertNotNull();

                    id = new SerializableDeclarationId( $"{parentId};Parameter={parameterSymbol.Ordinal}" );

                    return true;
                }

            case ITypeParameterSymbol typeParameterSymbol:
                {
                    var parentId = DocumentationCommentId.CreateDeclarationId( typeParameterSymbol.ContainingSymbol ).AssertNotNull();

                    id = new SerializableDeclarationId( $"{parentId};TypeParameter={typeParameterSymbol.Ordinal}" );

                    return true;
                }

            case IAssemblySymbol assemblySymbol:
                {
                    id = new SerializableDeclarationId( $"{_assemblyPrefix}{assemblySymbol.Identity}" );

                    return true;
                }

            case IModuleSymbol:
                {
                    id = default;

                    return false;
                }

            case INamedTypeSymbol:
                goto default;

            case ITypeSymbol typeSymbol:
                id = new SerializableDeclarationId( typeSymbol.GetSerializableTypeId().Id );

                return true;

            default:
                switch ( symbol.Kind )
                {
                    case SymbolKind.NamedType:
                    case SymbolKind.Method:
                    case SymbolKind.Field:
                    case SymbolKind.Assembly:
                    case SymbolKind.Event:
                    case SymbolKind.Namespace:
                    case SymbolKind.Parameter:
                    case SymbolKind.Property:
                    case SymbolKind.TypeParameter:
                        {
                            var documentationId = DocumentationCommentId.CreateDeclarationId( symbol );
                            
                            if ( documentationId == null )
                            {
                                id = default;

                                return false;
                            }

                            if ( targetKind == RefTargetKind.Default )
                            {
                                id = new SerializableDeclarationId( documentationId );
                            }
                            else
                            {
                                id = new SerializableDeclarationId( $"{documentationId};{targetKind}" );
                            }

                            return true;
                        }

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(symbol),
                            $"Cannot create a SerializableDeclarationId for '{symbol}' because it is a {symbol.Kind}." );
                }
        }
    }
}