// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code
{
    internal static class SignatureMatcher
    {
        /// <summary>
        /// Gets all members that match given requirements on signature.
        /// </summary>
        /// <typeparam name="TPayload">Payload type for the <paramref name="argumentGetter"/>.</typeparam>
        /// <typeparam name="TMember">Type of members.</typeparam>
        /// <param name="members">A collection of members.</param>
        /// <param name="payload">Payload object, passed to <paramref name="argumentGetter"/>.</param>
        /// <param name="name">Required name, or <see langword="null"/> if there is no requirement.</param>
        /// <param name="argumentCount">Required number of parameters, or <see langword="null"/> if there is no requirement.</param>
        /// <param name="argumentGetter">Predicate for matching parameters.</param>
        /// <param name="isStatic">Specifies whether the staticity should be matched (it is normally not a part of signature).</param>
        /// <returns>Enumeration of all members matching all conditions.</returns>
        public static IEnumerable<TMember> OfCompatibleSignature<TMember, TPayload>(
            this IMemberCollection<TMember> members,
            TPayload payload,
            string? name,
            int? argumentCount,
            Func<TPayload, int, (IType? Type, RefKind? RefKind)> argumentGetter,
            bool? isStatic )
            where TMember : class, IMethodBase
        {
            var compilation = members.DeclaringType.Compilation;

            return OfSignature(
                members,
                (payload, argumentGetter, compilation),
                name,
                argumentCount,
                IsMatchingParameter,
                isStatic,
                true );

            static bool IsMatchingParameter(
                (TPayload InnerPayload, Func<TPayload, int, (IType? Type, RefKind? RefKind)>? ArgumentGetter, ICompilation Compilation) payload,
                int parameterIndex,
                IType expectedType,
                RefKind expectedRefKind )
            {
                var parameterInfo = payload.ArgumentGetter?.Invoke( payload.InnerPayload, parameterIndex );

                if ( parameterInfo == null )
                {
                    return true;
                }

                return
                    (parameterInfo.Value.Type == null || payload.Compilation.Comparers.Default.IsConvertibleTo(
                        parameterInfo.Value.Type,
                        expectedType ))
                    && (parameterInfo.Value.RefKind == null || expectedRefKind == parameterInfo.Value.RefKind);
            }
        }

        /// <summary>
        /// Attempts to find a member with an exact match of the specified signature.
        /// </summary>
        /// <typeparam name="TPayload">Payload type for the <paramref name="parameterGetter"/>.</typeparam>
        /// <typeparam name="TMember">Type of members.</typeparam>
        /// <param name="members">A collection of members.</param>
        /// <param name="payload">Payload object, passed to <paramref name="parameterGetter"/>.</param>
        /// <param name="name">Required name of the method.</param>
        /// <param name="parameterCount">Required number of parameters.</param>
        /// <param name="parameterGetter">Delegate that gets <see cref="IType"/> and <see cref="RefKind"/> or a parameters on the gives index.</param>
        /// <param name="isStatic">Specifies whether the staticity should be matched (it is normally not a part of signature).</param>
        /// <returns>Member matching requirements or <see langword="null"/> if there is none.</returns>
        public static TMember? OfExactSignature<TMember, TPayload>(
            this IMemberCollection<TMember> members,
            TPayload payload,
            string? name,
            int parameterCount,
            Func<TPayload, int, (IType Type, RefKind RefKind)> parameterGetter,
            bool? isStatic )
            where TMember : class, IHasParameters
        {
            var compilation = members.DeclaringType.Compilation;

            var matching = OfSignature(
                members,
                (payload, parameterGetter, compilation),
                name,
                parameterCount,
                IsMatchingParameter,
                isStatic );

            // We use First and not Single because advices may provide many methods with same signature.
            // They do not make it to the final code, but they are stored in this class.
            return matching.FirstOrDefault();

            static bool IsMatchingParameter(
                (TPayload InnerPayload, Func<TPayload, int, (IType Type, RefKind RefKind)> ParameterGetter, ICompilation Compilation) payload,
                int parameterIndex,
                IType expectedType,
                RefKind expectedRefKind )
            {
                var parameterInfo = payload.ParameterGetter( payload.InnerPayload, parameterIndex );

                // TODO: This comparison does not work for generic type parameters.
                return
                    payload.Compilation.Comparers.Default.Equals( expectedType, parameterInfo.Type )
                    && expectedRefKind == parameterInfo.RefKind;
            }
        }

        /// <summary>
        /// Finds method bases in a list with signatures that match given arguments.
        /// </summary>
        /// <typeparam name="TPayload">Payload type for the <paramref name="argumentPredicate"/>.</typeparam>
        /// <typeparam name="TMember">Type of members.</typeparam>
        /// <param name="members">A collection of members.</param>
        /// <param name="payload">Payload object, passed to <paramref name="argumentPredicate"/>.</param>
        /// <param name="name">Required name, or <see langword="null"/> if there is no requirement.</param>
        /// <param name="argumentCount">Required number of arguments, or <see langword="null"/> if there is no requirement.</param>
        /// <param name="argumentPredicate">Predicate for matching arguments.</param>
        /// <param name="isStatic">Required staticity, or <see langword="null"/> if there is no requirement.</param>
        /// <param name="expandParams">If true, methods with <see langword="params" /> are treated as having the requested number of parameters if possible.</param>
        /// <returns>Enumeration of all members matching all conditions.</returns>
        private static IEnumerable<TMember> OfSignature<TMember, TPayload>(
            this IMemberCollection<TMember> members,
            TPayload payload,
            string? name,
            int? argumentCount,
            Func<TPayload, int, IType, RefKind, bool> argumentPredicate,
            bool? isStatic,
            bool expandParams = false )
            where TMember : class, IHasParameters
        {
            var candidates = name != null ? members.OfName( name ) : members;

            // Exclude any explicit interface implementation.
            // TODO: the Name be fully qualified, having it non-qualified is confusing and does not follow other implementations (28810).
            candidates = candidates.Where( c => !c.IsExplicitInterfaceImplementation );

            foreach ( var sourceItem in candidates )
            {
                if ( (isStatic != null && isStatic != sourceItem.IsStatic)
                     || (argumentCount != null && !expandParams && sourceItem.Parameters.Count != argumentCount)
                     || (argumentCount != null && expandParams && sourceItem.Parameters.Count > argumentCount + 1) )
                {
                    continue;
                }

                if ( argumentCount == null )
                {
                    yield return sourceItem;

                    continue;
                }

                var match = true;           // Determines whether the item matched all it's parameters, with exception of params.
                var tryMatchParams = false; // Determines whether the last parameter was params and whether we want to match rest of the arguments to it.

                if ( sourceItem.Parameters.Count > 0 )
                {
                    for ( var parameterIndex = 0; parameterIndex < sourceItem.Parameters.Count; parameterIndex++ )
                    {
                        var parameter = sourceItem.Parameters[parameterIndex];

                        if ( parameter.IsParams && expandParams && match
                             && !(parameterIndex < argumentCount && argumentPredicate( payload, parameterIndex, parameter.Type, RefKind.Ref )) )
                        {
                            if ( parameterIndex != sourceItem.Parameters.Count - 1 )
                            {
                                throw new InvalidOperationException( "Assertion failed." );
                            }

                            if ( expandParams )
                            {
                                tryMatchParams = true;
                            }
                        }

                        if ( parameterIndex >= argumentCount )
                        {
                            match = false;

                            break;
                        }

                        if ( !argumentPredicate( payload, parameterIndex, parameter.Type, parameter.RefKind ) )
                        {
                            match = false;

                            break;
                        }
                    }
                }

                if ( match )
                {
                    if ( !tryMatchParams && argumentCount != sourceItem.Parameters.Count )
                    {
                        // Will not be matching params and parameter counts don't match.
                        continue;
                    }

                    yield return sourceItem;
                }
                else if ( tryMatchParams )
                {
                    // Attempt to match C# params - all remaining parameter types should be assignable to the array element type.
                    var parameterType = sourceItem.Parameters[sourceItem.Parameters.Count - 1].Type;
                    var elementType = GetParamsElementType( parameterType );

                    if ( elementType == null )
                    {
                        continue;
                    }

                    var paramsMatch = true;

                    for ( var i = sourceItem.Parameters.Count - 1; i < argumentCount; i++ )
                    {
                        if ( !argumentPredicate( payload, i, elementType, RefKind.None ) )
                        {
                            paramsMatch = false;

                            break;
                        }
                    }

                    if ( paramsMatch )
                    {
                        yield return sourceItem;
                    }
                }
            }
        }

        private static IType? GetParamsElementType( IType type )
        {
            // See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-13.0/params-collections#method-parameters.

            return type switch
            {
                IArrayType { Rank: 1 } arrayType => arrayType.ElementType,
                INamedType { FullName: "System.Span" or "System.ReadOnlySpan" } namedType
                    => namedType.TypeArguments[0],
                INamedType namedType when namedType.Attributes.Any( a => a.Type.FullName == "System.Runtime.CompilerServices.CollectionBuilderAttribute" )
                    => GetIterationType( namedType ),
                INamedType { TypeKind: TypeKind.Struct or TypeKind.Class or TypeKind.RecordStruct or TypeKind.RecordClass } namedType when
                    namedType.AllImplementedInterfaces.Any( i => i.FullName == "System.Collections.Generic.IEnumerable" )
                    => GetIterationType( namedType ),
                INamedType
                    {
                        TypeKind: TypeKind.Interface,
                        FullName: "System.Collections.Generic.IEnumerable" or "System.Collections.Generic.IReadOnlyCollection"
                        or "System.Collections.Generic.IReadOnlyList" or "System.Collections.Generic.ICollection" or "System.Collections.Generic.IList"
                    } namedType
                    => namedType.TypeArguments[0],
                _ => null
            };
        }

        private static IType? GetIterationType( INamedType collectionType )
        {
            // This determination of iteration type is specific to collection expressions and params collections.
            // The iteration type for foreach would also need to consider extension method GetEnumerator.

            // This is based on https://github.com/dotnet/csharpstandard/blob/draft-v9/standard/statements.md#1395-the-foreach-statement,
            // but see https://github.com/dotnet/csharpstandard/issues/1188 (we follow the compiler, not the spec, where the two disagree).

            var getEnumeratorMethods = collectionType.Methods.OfCompatibleSignature( nameof(IEnumerable.GetEnumerator), argumentTypes: [], isStatic: false )
                .Where( m => m.Accessibility == Accessibility.Public )
                .ToArray();

            if ( getEnumeratorMethods is [var getEnumeratorMethod] )
            {
                var enumeratorType = getEnumeratorMethod.ReturnType as INamedType;

                return enumeratorType?.Properties.OfName( "Current" ).SingleOrDefault()?.Type;
            }
            else
            {
                var genericEnumerableInterfaces = collectionType.AllImplementedInterfaces
                    .Where( i => i.FullName == "System.Collections.Generic.IEnumerable" )
                    .ToArray();

                switch ( genericEnumerableInterfaces.Length )
                {
                    case 0:
                        break;

                    case 1:
                        return genericEnumerableInterfaces[0].TypeArguments[0];

                    case > 1:
                        return null;
                }

                var hasNonGenericEnumerableInterface = collectionType.AllImplementedInterfaces
                    .Any( i => i.FullName == "System.Collections.IEnumerable" );

                if ( hasNonGenericEnumerableInterface )
                {
                    return ((ICompilationInternal) collectionType.Compilation).Factory.GetSpecialType( SpecialType.Object );
                }

                return null;
            }
        }
    }
}