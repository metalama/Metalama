// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Comparers
{
    /// <summary>
    /// Compares symbols for purposes of method signature comparison. This comparer ignores the containing declaration of type parameters,
    /// allowing to compare interface methods with their implementation. IT IS MOST PROBABLY INCORRECT - a proper way of solving this problem
    /// is to map the type of interface parameters to the type, then do a normal comparison.
    /// </summary>
    internal sealed class SignatureTypeComparer : IEqualityComparer<ISymbol?>, IEqualityComparer<IType>
    {
        public static SignatureTypeComparer Instance { get; } = new( StructuralSymbolComparer.Default );

        private readonly StructuralSymbolComparer _inner;

        private SignatureTypeComparer( StructuralSymbolComparer inner )
        {
            this._inner = inner;
        }

        public bool Equals( ISymbol? left, ISymbol? right )
        {
            switch (left, right)
            {
                case (IArrayTypeSymbol leftArray, IArrayTypeSymbol rightArray):
                    return leftArray.Rank == rightArray.Rank
                           && this.Equals( leftArray.ElementType, rightArray.ElementType );

                case (IDynamicTypeSymbol, IDynamicTypeSymbol):
                    return true;

                case (ITypeParameterSymbol leftTypeParameter, ITypeParameterSymbol rightTypeParameter):
                    // TODO: Interpret compile-time parameters for generic interfaces.
                    return
                        leftTypeParameter.TypeParameterKind == rightTypeParameter.TypeParameterKind
                        && leftTypeParameter.Ordinal == rightTypeParameter.Ordinal;

                case (IPointerTypeSymbol leftPointerType, IPointerTypeSymbol rightPointerType):
                    // TODO: Constraints.
                    return this.Equals( leftPointerType.PointedAtType, rightPointerType.PointedAtType );

                case (IFunctionPointerTypeSymbol leftFunctionPointerType, IFunctionPointerTypeSymbol rightFunctionPointerType):
                    return
                        this.Equals( leftFunctionPointerType.Signature, rightFunctionPointerType.Signature );

                case (IParameterSymbol leftParameter, IParameterSymbol rightParameter):
                    return
                        this.Equals( leftParameter.Type, rightParameter.Type )
                        && leftParameter.RefKind == rightParameter.RefKind;

                case (IMethodSymbol leftMethod, IMethodSymbol rightMethod):
                    // Whole method signature matching.
                    return
                        StringComparer.Ordinal.Equals( leftMethod.Name, rightMethod.Name )
                        && leftMethod.TypeParameters.Length == rightMethod.TypeParameters.Length
                        && this.Equals( leftMethod.ReturnType, rightMethod.ReturnType )
                        && leftMethod.Parameters.SequenceEqual( rightMethod.Parameters, ( l, r ) => this.Equals( l, r ) );

                case (IEventSymbol leftEvent, IEventSymbol rightEvent):
                    return
                        StringComparer.Ordinal.Equals( leftEvent.Name, rightEvent.Name )
                        && this.Equals( leftEvent.Type, rightEvent.Type );

                case (IPropertySymbol leftProperty, IPropertySymbol rightProperty):
                    return
                        StringComparer.Ordinal.Equals( leftProperty.Name, rightProperty.Name )
                        && this.Equals( leftProperty.Type, rightProperty.Type )
                        && leftProperty.Parameters.SequenceEqual( rightProperty.Parameters, ( l, r ) => this.Equals( l, r ) );

                case (INamedTypeSymbol leftNamedType, INamedTypeSymbol rightNamedType):
                    return
                        this._inner.Equals( leftNamedType.OriginalDefinition, rightNamedType.OriginalDefinition )
                        && leftNamedType.TypeArguments.SequenceEqual(
                            rightNamedType.TypeArguments,
                            ( l, r ) => this.Equals( l, r ) );

                default:
                    return this._inner.Equals( left, right );
            }
        }

        public int GetHashCode( ISymbol? obj )
        {
            switch ( obj )
            {
                case null:
                    return 0;

                case IArrayTypeSymbol array:
                    return HashCode.Combine( this.GetHashCode( array ) );

                case IDynamicTypeSymbol:
                    return HashCode.Combine( 0x57446317 );

                case ITypeParameterSymbol typeParameter:
                    return HashCode.Combine( typeParameter.TypeParameterKind, typeParameter.Ordinal );

                case IPointerTypeSymbol pointerType:
                    return HashCode.Combine( this.GetHashCode( pointerType.PointedAtType ) );

                case IFunctionPointerTypeSymbol functionPointerType:
                    return
                        HashCode.Combine( this.GetHashCode( functionPointerType.Signature ) );

                case IParameterSymbol parameterSymbol:
                    return HashCode.Combine( parameterSymbol.Ordinal );

                case IMethodSymbol method:
                    // Whole method signature matching.
                    return
                        HashCode.Combine(
                            method.Name,
                            method.TypeParameters.Length,
                            method.Parameters.Length );

                case INamedTypeSymbol namedType:
                    return
                        HashCode.Combine( namedType.MetadataName );

                default:
                    return this._inner.GetHashCode( obj );
            }
        }

        public bool Equals( IType? left, IType? right )
        {
            if ( ReferenceEquals( left, right ) )
            {
                return true;
            }
            else if ( left == null || right == null )
            {
                return false;
            }
            else if ( left is ISymbolBasedCompilationElement { SymbolMustBeMapped: false, Symbol: { } xSymbol } &&
                      right is ISymbolBasedCompilationElement { SymbolMustBeMapped: false, Symbol: { } ySymbol } )
            {
                // Fast path.
                return this.Equals( xSymbol, ySymbol );
            }
            else
            {
                switch (left, right)
                {
                    case (INamedType leftNamedType, INamedType rightNamedType):
                        return
                            leftNamedType.Definition.Equals( rightNamedType.Definition )
                            && leftNamedType.TypeArguments.SequenceEqual(
                                rightNamedType.TypeArguments,
                                this );

                    case (IArrayType leftArray, IArrayType rightArray):
                        return leftArray.Rank == rightArray.Rank
                               && this.Equals( leftArray.ElementType, rightArray.ElementType );

                    case (IDynamicType, IDynamicType):
                        return true;

                    case (ITypeParameter leftTypeParameter, ITypeParameter rightTypeParameter):
                        // TODO: Interpret compile-time parameters for generic interfaces.
                        return
                            leftTypeParameter.TypeParameterKind == rightTypeParameter.TypeParameterKind
                            && leftTypeParameter.Index == rightTypeParameter.Index;

                    case (IPointerType leftPointerType, IPointerType rightPointerType):
                        // TODO: Constraints.
                        return this.Equals( leftPointerType.PointedAtType, rightPointerType.PointedAtType );

                    case (IFunctionPointerType, IFunctionPointerType):
                        throw new NotImplementedException( UnsupportedFeatures.IntroducedFunctionPointerComparison );

                    default:
                        return false;
                }
            }
        }

        public int GetHashCode( IType obj )
        {
            if ( obj is ISymbolBasedCompilationElement { SymbolMustBeMapped: false } symbolType )
            {
                // Fast path.
                return this.GetHashCode( symbolType.Symbol );
            }
            else
            {
                switch ( obj )
                {
                    case null:
                        return 0;

                    case IArrayType array:
                        return HashCode.Combine( this.GetHashCode( array ) );

                    case IDynamicType:
                        return HashCode.Combine( 0x57446317 );

                    case ITypeParameter typeParameter:
                        return HashCode.Combine( typeParameter.TypeParameterKind, typeParameter.Index );

                    case IPointerType pointerType:
                        return HashCode.Combine( this.GetHashCode( pointerType.PointedAtType ) );

                    case IFunctionPointerType:
                        throw new NotImplementedException( UnsupportedFeatures.IntroducedFunctionPointerComparison );

                    case INamedType namedType:
                        return
                            HashCode.Combine( namedType.FullName, namedType.TypeParameters.Count );

                    default:
                        throw new AssertionFailedException();
                }
            }
        }
    }
}