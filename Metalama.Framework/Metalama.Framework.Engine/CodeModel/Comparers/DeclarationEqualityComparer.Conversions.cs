// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RoslynSpecialType = Microsoft.CodeAnalysis.SpecialType;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;
using VarianceKind = Metalama.Framework.Code.VarianceKind;

namespace Metalama.Framework.Engine.CodeModel.Comparers;

internal partial class DeclarationEqualityComparer
{
    private sealed class Conversions( DeclarationEqualityComparer parent )
    {
        private static StructuralDeclarationComparer Comparer => StructuralDeclarationComparer.Default;

        // Largely based on Roslyn's ClassifyConversion.
        internal bool HasConversion( IType left, IType right, ConversionKind kind )
        {
            Invariant.Assert( kind is ConversionKind.Default or ConversionKind.Reference or ConversionKind.Implicit );

            // Identity conversion and implicit reference conversion.
            if ( this.HasIdentityOrImplicitReferenceConversion( left, right ) )
            {
                return true;
            }

            if ( kind is ConversionKind.Default or ConversionKind.Implicit )
            {
                // Nullable (value type) conversion.
                if ( right.IsNullableValueType() )
                {
                    var unwrappedRight = ((INamedType) right).TypeArguments.Single();

                    if ( Comparer.Equals( left, unwrappedRight ) )
                    {
                        return true;
                    }
                }

                // Boxing conversion.
                if ( this.HasBoxingConversion( left, right ) )
                {
                    return true;
                }
            }

            if ( kind is ConversionKind.Implicit )
            {
                if ( this.HasUserDefinedImplicitConversion( left, right ) )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsClass( IType type ) => type.TypeKind is TypeKind.Class or TypeKind.RecordClass;

        private bool HasIdentityOrImplicitReferenceConversion( IType left, IType right )
        {
            if ( Comparer.Equals( left, right ) )
            {
                return true;
            }

            if ( left.IsReferenceType == true )
            {
                // From any reference type to object and dynamic.
                if ( right.SpecialType == SpecialType.Object || right is IDynamicType )
                {
                    return true;
                }

                switch ( left.TypeKind )
                {
                    case TypeKind.Class or TypeKind.RecordClass:
                        if ( IsBaseClass( left, right ) )
                        {
                            return true;
                        }

                        return this.HasImplicitConversionToInterface( left, right );

                    case TypeKind.Interface:
                        return this.HasImplicitConversionToInterface( left, right );

                    case TypeKind.Delegate:
                        // From any delegate-type to System.Delegate, System.MulticastDelegate and the interfaces it implements.
                        if ( right.GetSymbol()?.SpecialType is RoslynSpecialType.System_Delegate or RoslynSpecialType.System_MulticastDelegate
                             || this.HasImplicitConversionToInterface(
                                 left.GetCompilationModel().Factory.GetTypeByReflectionType( typeof(MulticastDelegate) ),
                                 right ) )
                        {
                            return true;
                        }

                        if ( right is INamedType rightNamedType && this.HasVarianceConversion( (INamedType) left, rightNamedType ) )
                        {
                            return true;
                        }

                        return false;

                    case TypeKind.TypeParameter:
                        var leftTypeParameter = (ITypeParameter) left;

                        foreach ( var constraint in leftTypeParameter.TypeConstraints )
                        {
                            if ( this.HasIdentityOrImplicitReferenceConversion( constraint, right ) )
                            {
                                return true;
                            }
                        }

                        return false;

                    case TypeKind.Array:
                        return this.HasImplicitConversionFromArray( (IArrayType) left, right );
                }
            }

            return false;
        }

        private static bool IsBaseClass( IType left, IType right )
        {
            if ( !IsClass( right ) )
            {
                return false;
            }

            for ( var current = left; current != null; current = current.GetBaseType() )
            {
                if ( Comparer.Equals( current, right ) )
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasImplicitConversionToInterface( IType left, IType right )
        {
            if ( right.TypeKind != TypeKind.Interface )
            {
                return false;
            }

            if ( IsClass( left ) )
            {
                return this.HasAnyBaseInterfaceConversion( left, right );
            }

            if ( left.TypeKind == TypeKind.Interface )
            {
                return this.HasAnyBaseInterfaceConversion( left, right ) ||
                       this.HasVarianceConversion( left.AssertCast<INamedType>(), right.AssertCast<INamedType>() );
            }

            return false;
        }

        private bool HasAnyBaseInterfaceConversion( IType left, IType right )
        {
            if ( right.TypeKind != TypeKind.Interface )
            {
                return false;
            }

            if ( left is not INamedType leftType )
            {
                return false;
            }

            foreach ( var @interface in leftType.AllImplementedInterfaces )
            {
                if ( this.HasVarianceConversion( @interface, right.AssertCast<INamedType>() ) )
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasVarianceConversion( INamedType left, INamedType right )
        {
            if ( Comparer.Equals( left, right ) )
            {
                return true;
            }

            if ( !Comparer.Equals( left.Definition, right.Definition ) )
            {
                return false;
            }

            var typeParameters = left.TypeParameters;
            var leftTypeArguments = left.TypeArguments;
            var rightTypeArguments = right.TypeArguments;

            for ( var i = 0; i < typeParameters.Count; i++ )
            {
                var leftTypeArgument = leftTypeArguments[i];
                var rightTypeArgument = rightTypeArguments[i];

                // If they're identical then this one is automatically good, so skip it.
                if ( Comparer.Equals( leftTypeArgument, rightTypeArgument ) )
                {
                    continue;
                }

                var typeParameter = typeParameters[i];

                switch ( typeParameter.Variance )
                {
                    case VarianceKind.None:
                        return false;

                    case VarianceKind.Out:
                        if ( !this.HasIdentityOrImplicitReferenceConversion( leftTypeArgument, rightTypeArgument ) )
                        {
                            return false;
                        }

                        break;

                    case VarianceKind.In:
                        if ( !this.HasIdentityOrImplicitReferenceConversion( rightTypeArgument, leftTypeArgument ) )
                        {
                            return false;
                        }

                        break;

                    default:
                        throw new AssertionFailedException( $"Unexpected variance kind: {typeParameter.Variance}" );
                }
            }

            return true;
        }

        private bool HasImplicitConversionFromArray( IArrayType left, IType right )
        {
            // Covariant array conversion.
            if ( right is IArrayType rightArray && left.Rank == rightArray.Rank
                                                && this.HasIdentityOrImplicitReferenceConversion( left.ElementType, rightArray.ElementType ) )
            {
                return true;
            }

            // From any array-type to System.Array and the interfaces it implements.
            if ( right.GetSymbol()?.SpecialType == RoslynSpecialType.System_Array
                 || this.HasImplicitConversionToInterface( left.GetCompilationModel().Factory.GetTypeByReflectionType( typeof(Array) ), right ) )
            {
                return true;
            }

            // From a single-dimensional array type to IList<T> and other collection interfaces.
            if ( left.Rank == 1 )
            {
                if ( right.GetSymbol() is { } symbol && SymbolHelpers.ArrayGenericInterfaces.Contains( symbol.SpecialType ) )
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasBoxingConversion( IType left, IType right )
        {
            if ( left is ITypeParameter leftTypeParameter && left.IsReferenceType == true )
            {
                if ( right.SpecialType == SpecialType.Object || right is IDynamicType )
                {
                    return true;
                }

                foreach ( var constraint in leftTypeParameter.TypeConstraints )
                {
                    if ( this.HasIdentityOrImplicitReferenceConversion( constraint, right ) )
                    {
                        return true;
                    }
                }
            }

            if ( left.IsReferenceType != false || right.IsReferenceType != true )
            {
                return false;
            }

            if ( left.IsNullable == true )
            {
                return this.HasBoxingConversion( ((INamedType) left).TypeArguments[0], right );
            }

            if ( left is INamedType { IsRef: true } )
            {
                return false;
            }

            if ( IsBaseClass( left, right ) )
            {
                return true;
            }

            if ( this.HasAnyBaseInterfaceConversion( left, right ) )
            {
                return true;
            }

            return false;
        }

        private bool HasUserDefinedImplicitConversion( IType left, IType right )
        {
            var typeSet = GetTypesParticipatingInUserDefinedConversion( left, includeBaseTypes: true )
                .Union( GetTypesParticipatingInUserDefinedConversion( right, includeBaseTypes: false ) );

            var conversions = this.ComputeApplicableUserDefinedImplicitConversionSet( left, right, typeSet ).ToImmutableArray();

            if ( conversions.Length == 0 )
            {
                return false;
            }

            var mostSpecificSourceType = this.MostSpecificSourceType( left, conversions );

            if ( mostSpecificSourceType == null )
            {
                return false;
            }

            var mostSpecificTargetType = this.MostSpecificTargetType( right, conversions );

            if ( mostSpecificTargetType == null )
            {
                return false;
            }

            return HasMostSpecificConversionOperator( mostSpecificSourceType, mostSpecificTargetType, conversions );
        }

        private static IEnumerable<INamedType> GetTypesParticipatingInUserDefinedConversion( IType type, bool includeBaseTypes )
        {
            type = type.ToNonNullable();

            if ( type is ITypeParameter typeParameter )
            {
                foreach ( var constraint in typeParameter.TypeConstraints )
                {
                    if ( constraint.TypeKind == TypeKind.Interface )
                    {
                        continue;
                    }

                    foreach ( var result in GetFromClassOrStruct( constraint ) )
                    {
                        yield return result;
                    }
                }
            }
            else
            {
                foreach ( var result in GetFromClassOrStruct( type ) )
                {
                    yield return result;
                }
            }

            // ReSharper disable once VariableHidesOuterVariable
            IEnumerable<INamedType> GetFromClassOrStruct( IType type )
            {
                if ( type.TypeKind is TypeKind.Class or TypeKind.RecordClass or TypeKind.Struct or TypeKind.RecordStruct )
                {
                    var namedType = (INamedType) type;

                    yield return namedType;
                }

                if ( !includeBaseTypes )
                {
                    yield break;
                }

                for ( var current = type.GetBaseType(); current != null; current = current.GetBaseType() )
                {
                    yield return current;
                }
            }
        }

        private IEnumerable<UserDefinedImplicitConversion> ComputeApplicableUserDefinedImplicitConversionSet(
            IType left,
            IType right,
            IEnumerable<INamedType> typeSet )
        {
            if ( left.TypeKind == TypeKind.Interface || right.TypeKind == TypeKind.Interface )
            {
                yield break;
            }

            foreach ( var type in typeSet )
            {
                foreach ( var op in GetCandidatesFromType( type ) )
                {
                    yield return op;
                }
            }

            IEnumerable<UserDefinedImplicitConversion> GetCandidatesFromType( INamedType type )
            {
                foreach ( var op in type.Methods.OfName( WellKnownMemberNames.ImplicitConversionName ) )
                {
                    Invariant.Assert( op.OperatorKind == OperatorKind.ImplicitConversion );

                    // We might have a bad operator and be in an error recovery situation. Ignore it.
                    if ( op.ReturnType.SpecialType == SpecialType.Void || op.Parameters.Count != 1 )
                    {
                        continue;
                    }

                    var convertsFrom = op.Parameters[0].Type;
                    var convertsTo = op.ReturnType;

                    var hasFromConversion = parent.IsConvertibleTo( left, convertsFrom, ConversionKind.Default );
                    var hasToConversion = parent.IsConvertibleTo( convertsTo, right, ConversionKind.Default );

                    if ( hasFromConversion && hasToConversion )
                    {
                        var liftingCount = 0;

                        if ( right.IsNullableValueType() && IsValidNullableValueTypeArgument( convertsTo ) )
                        {
                            convertsTo = convertsTo.ToNullable();
                            liftingCount = 1;
                        }

                        yield return new UserDefinedImplicitConversion( convertsFrom, convertsTo, liftingCount );
                    }
                    else if ( left.IsNullableValueType()
                              && IsValidNullableValueTypeArgument( convertsFrom )
                              && (right.IsReferenceType == true || right.IsNullable == true) )
                    {
                        var nullableFrom = convertsFrom.ToNullable();
                        var liftingCount = 1;

                        var nullableTo = convertsTo;

                        if ( IsValidNullableValueTypeArgument( nullableTo ) )
                        {
                            nullableTo = nullableTo.ToNullable();
                            liftingCount = 2;
                        }

                        var hasLiftedFromConversion = parent.IsConvertibleTo( left, nullableFrom, ConversionKind.Default );
                        var hasLiftedToConversion = parent.IsConvertibleTo( nullableTo, right, ConversionKind.Default );

                        if ( hasLiftedFromConversion && hasLiftedToConversion )
                        {
                            yield return new UserDefinedImplicitConversion( nullableFrom, nullableTo, liftingCount );
                        }
                    }
                }
            }

            static bool IsValidNullableValueTypeArgument( IType type ) => type is INamedType { IsReferenceType: false, IsNullable: false, IsRef: false };
        }

        private IType? MostSpecificSourceType( IType type, ImmutableArray<UserDefinedImplicitConversion> conversions )
        {
            // If any of the operators in U convert from S then SX is S.
            if ( conversions.Any( conv => Comparer.Equals( conv.From, type ) ) )
            {
                return type;
            }

            // Otherwise, SX is the most encompassed type in the set of source types of the operators in U.
            return UniqueBest(
                conversions,
                conv => conv.From,
                ( left, right ) =>
                {
                    if ( Comparer.Equals( left, right ) )
                    {
                        return BetterResult.Equal;
                    }

                    var leftWins = parent.IsConvertibleTo( left, right, ConversionKind.Default );
                    var rightWins = parent.IsConvertibleTo( right, left, ConversionKind.Default );

                    return
                        leftWins == rightWins ? BetterResult.Neither
                        : leftWins ? BetterResult.Left
                        : BetterResult.Right;
                } );
        }

        private IType? MostSpecificTargetType( IType type, ImmutableArray<UserDefinedImplicitConversion> conversions )
        {
            // If any of the operators in U convert to T then TX is T.
            if ( conversions.Any( conv => Comparer.Equals( conv.To, type ) ) )
            {
                return type;
            }

            // Otherwise, TX is the most encompassing type in the set of target types of the operators in U.
            return UniqueBest(
                conversions,
                conv => conv.To,
                ( left, right ) =>
                {
                    if ( Comparer.Equals( left, right ) )
                    {
                        return BetterResult.Equal;
                    }

                    var leftWins = parent.IsConvertibleTo( right, left, ConversionKind.Default );
                    var rightWins = parent.IsConvertibleTo( left, right, ConversionKind.Default );

                    return
                        leftWins == rightWins ? BetterResult.Neither
                        : leftWins ? BetterResult.Left
                        : BetterResult.Right;
                } );
        }

        private static bool HasMostSpecificConversionOperator(
            IType mostSpecificSourceType,
            IType mostSpecificTargetType,
            ImmutableArray<UserDefinedImplicitConversion> conversions )
        {
            bool TypesAreEqual( UserDefinedImplicitConversion conv )
                => Comparer.Equals( conv.From, mostSpecificSourceType ) && Comparer.Equals( conv.To, mostSpecificTargetType );

            var hasUnlifted = HasUnique( conversions, conv => TypesAreEqual( conv ) && conv.LiftingCount == 0 );

            switch ( hasUnlifted )
            {
                case true:
                    return true;

                case null:
                    return false;
            }

            var hasHalfLifted = HasUnique( conversions, conv => TypesAreEqual( conv ) && conv.LiftingCount == 1 );

            switch ( hasHalfLifted )
            {
                case true:
                    return true;

                case null:
                    return false;
            }

            var hasFullyLifted = HasUnique( conversions, conv => TypesAreEqual( conv ) && conv.LiftingCount == 2 );

            switch ( hasFullyLifted )
            {
                case true:
                    return true;

                case null:
                    return false;
            }

            return false;
        }

        private static TResult? UniqueBest<TItem, TResult>(
            ImmutableArray<TItem> items,
            Func<TItem, TResult> extract,
            Func<TResult, TResult, BetterResult> better )
            where TResult : class
        {
            if ( items.IsEmpty )
            {
                return null;
            }

            int? candidateIndex = null;
            TResult? candidateResult = null;

            for ( var currentIndex = 0; currentIndex < items.Length; currentIndex++ )
            {
                var currentResult = extract( items[currentIndex] );

                if ( candidateIndex == null )
                {
                    candidateIndex = currentIndex;
                    candidateResult = currentResult;

                    continue;
                }

                var result = better( candidateResult!, currentResult );

                switch ( result )
                {
                    case BetterResult.Equal:
                        // The list had the same item twice. Just ignore it.
                        continue;

                    case BetterResult.Neither:
                        // Neither the current item nor the candidate item are better,
                        // and therefore neither of them can be the best. We no longer
                        // have a candidate for best item.
                        candidateIndex = null;
                        candidateResult = null;

                        break;

                    case BetterResult.Right:
                        // The candidate is worse than the current item, so replace it
                        // with the current item.
                        candidateIndex = currentIndex;
                        candidateResult = currentResult;

                        break;
                }

                // Otherwise, the candidate is better than the current item, so
                // it continues to be the candidate.
            }

            if ( candidateIndex == null )
            {
                return null;
            }

            // We had a candidate that was better than everything that came *after* it.
            // Now verify that it was better than everything that came before it.

            for ( var currentIndex = 0; currentIndex < candidateIndex.Value; currentIndex++ )
            {
                var currentItem = extract( items[currentIndex] );

                var result = better( candidateResult!, currentItem );

                if ( result != BetterResult.Left && result != BetterResult.Equal )
                {
                    // The candidate was not better than everything that came before it. There is 
                    // no best item.
                    return null;
                }
            }

            // The candidate was better than everything that came before it.

            return candidateResult;
        }

        /// <returns>true: has a unique item matching predicate; false: has no matching items; null: has more than one matching item.</returns>
        private static bool? HasUnique<T>( ImmutableArray<T> items, Func<T, bool> predicate )
        {
            if ( items.IsEmpty )
            {
                return false;
            }

            var foundItem = false;

            foreach ( var item in items )
            {
                if ( predicate( item ) )
                {
                    if ( foundItem )
                    {
                        // Not unique.
                        return null;
                    }
                    else
                    {
                        foundItem = true;
                    }
                }
            }

            return foundItem;
        }

        private record struct UserDefinedImplicitConversion( IType From, IType To, int LiftingCount );

        private enum BetterResult
        {
            Left,
            Right,
            Neither,
            Equal
        }
    }
}