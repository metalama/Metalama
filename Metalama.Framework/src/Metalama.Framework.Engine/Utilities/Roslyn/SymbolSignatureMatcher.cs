// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Symbol version of <see cref="Code.SignatureMatcher"/>.
/// </summary>
internal static class SymbolSignatureMatcher
{
    /// <summary>
    /// Gets the list of members with signatures compatible with the signature of the specified member.
    /// </summary>
    public static IEnumerable<TSymbol> GetMembersOfCompatibleSignature<TSymbol>(
        this INamedTypeSymbol containingType,
        CompilationContext containingTypeCompilation,
        TSymbol compatibleSymbol,
        CompilationContext compatibleSymbolCompilation )
        where TSymbol : ISymbol
    {
        return containingType.GetMembersOfCompatibleSignature<TSymbol, ImmutableArray<IParameterSymbol>>(
            containingTypeCompilation,
            compatibleSymbol.GetParameters(),
            compatibleSymbol.Name,
            compatibleSymbol.GetParameters().Length,
            GetParameter,
            compatibleSymbolCompilation,
            compatibleSymbol.IsStatic );

        static (ITypeSymbol? Type, RefKind? RefKind) GetParameter( ImmutableArray<IParameterSymbol> parameters, int index )
        {
            var parameter = parameters[index];

            return (parameter.Type, parameter.RefKind);
        }
    }

    /// <summary>
    /// Gets the list of methods with signatures compatible with specified constraints.
    /// </summary>
    /// <param name="name">Name of the method.</param>
    /// <param name="argumentTypes">Constraint on reflection types of arguments. <c>Null</c>items in the list signify any type.</param>
    /// <param name="isStatic">Constraint on staticity of the method.</param>
    /// <returns>Enumeration of methods matching specified constraints.</returns>
    public static IEnumerable<IMethodSymbol> GetMethodsOfCompatibleSignature(
        this INamedTypeSymbol containingType,
        CompilationContext containingTypeCompilation,
        string name,
        IReadOnlyList<ITypeSymbol?>? argumentTypes,
        CompilationContext argumentTypesCompilation,
        bool? isStatic = false )
    {
        return containingType.GetMembersOfCompatibleSignature<IMethodSymbol, IReadOnlyList<ITypeSymbol?>?>(
            containingTypeCompilation,
            argumentTypes,
            name,
            argumentTypes?.Count,
            GetParameter,
            argumentTypesCompilation,
            isStatic );

        static (ITypeSymbol? Type, RefKind? RefKind) GetParameter( IReadOnlyList<ITypeSymbol?>? argumentTypes, int index )
            => argumentTypes?[index] != null
                ? (argumentTypes[index]!, null)
                : (null, null);
    }

    /// <summary>
    /// Gets all members that match given requirements on signature.
    /// </summary>
    /// <typeparam name="TPayload">Payload type for the <paramref name="argumentGetter"/>.</typeparam>
    /// <typeparam name="TMember">Type of members.</typeparam>
    /// <param name="payload">Payload object, passed to <paramref name="argumentGetter"/>.</param>
    /// <param name="name">Required name, or <see langword="null"/> if there is no requirement.</param>
    /// <param name="argumentCount">Required number of parameters, or <see langword="null"/> if there is no requirement.</param>
    /// <param name="argumentGetter">Predicate for matching parameters.</param>
    /// <param name="isStatic">Specifies whether the staticity should be matched (it is normally not a part of signature).</param>
    /// <returns>Enumeration of all members matching all conditions.</returns>
    private static IEnumerable<TMember> GetMembersOfCompatibleSignature<TMember, TPayload>(
        this INamedTypeSymbol containingType,
        CompilationContext containingTypeCompilation,
        TPayload payload,
        string? name,
        int? argumentCount,
        Func<TPayload, int, (ITypeSymbol? Type, RefKind? RefKind)> argumentGetter,
        CompilationContext argumentTypesCompilation,
        bool? isStatic )
        where TMember : ISymbol
    {
        return GetMembersOfSignature<TMember, (TPayload InnerPayload, Func<TPayload, int, (ITypeSymbol? Type, RefKind? RefKind)> ArgumentGetter, CompilationContext Compilation)>(
            containingType,
            containingTypeCompilation,
            (payload, argumentGetter, argumentTypesCompilation),
            name,
            argumentCount,
            IsMatchingParameter,
            isStatic,
            true );

        static bool IsMatchingParameter(
            (TPayload InnerPayload, Func<TPayload, int, (ITypeSymbol? Type, RefKind? RefKind)> ArgumentGetter, CompilationContext ArgumentCompilation) payload,
            int parameterIndex,
            ITypeSymbol expectedType,
            RefKind expectedRefKind )
        {
            var parameterInfo = payload.ArgumentGetter.Invoke( payload.InnerPayload, parameterIndex );

            var translatedExpectedType = payload.ArgumentCompilation.SymbolTranslator.Translate( expectedType );

            var parameterMatches =
                (parameterInfo.Type == null ||
                    (translatedExpectedType != null && payload.ArgumentCompilation.SymbolComparer.IsConvertibleTo( parameterInfo.Type, translatedExpectedType )))
                && (parameterInfo.RefKind == null || expectedRefKind == parameterInfo.RefKind);

            if ( !parameterMatches )
            {
                // InterpolatedStringHandlers have been added in a backward-compatible way.
                if ( parameterInfo.RefKind == RefKind.Ref
                    && parameterInfo.Type?.GetAttributes().Any( a => a.AttributeClass?.Name == "InterpolatedStringHandlerAttribute" ) == true
                    && expectedRefKind == RefKind.None
                    && expectedType.SpecialType == SpecialType.System_String )
                {
                    parameterMatches = true;
                }
            }

            return parameterMatches;
        }
    }

    /// <summary>
    /// Finds method bases in a list with signatures that match given arguments.
    /// </summary>
    /// <typeparam name="TPayload">Payload type for the <paramref name="argumentPredicate"/>.</typeparam>
    /// <typeparam name="TMember">Type of members.</typeparam>
    /// <param name="payload">Payload object, passed to <paramref name="argumentPredicate"/>.</param>
    /// <param name="name">Required name, or <see langword="null"/> if there is no requirement.</param>
    /// <param name="argumentCount">Required number of arguments, or <see langword="null"/> if there is no requirement.</param>
    /// <param name="argumentPredicate">Predicate for matching arguments.</param>
    /// <param name="isStatic">Required staticity, or <see langword="null"/> if there is no requirement.</param>
    /// <param name="expandParams">If true, methods with <see langword="params" /> are treated as having the requested number of parameters if possible.</param>
    /// <returns>Enumeration of all members matching all conditions.</returns>
    private static IEnumerable<TMember> GetMembersOfSignature<TMember, TPayload>(
        this INamedTypeSymbol containingType,
        CompilationContext compilation,
        TPayload payload,
        string? name,
        int? argumentCount,
        Func<TPayload, int, ITypeSymbol, RefKind, bool> argumentPredicate,
        bool? isStatic,
        bool expandParams = false )
        where TMember : ISymbol
    {
        var members = name != null ? containingType.GetMembers( name ) : containingType.GetMembers();
        var candidates = members.OfType<TMember>();

        // Exclude any explicit interface implementation.
        // TODO: the Name be fully qualified, having it non-qualified is confusing and does not follow other implementations (28810).
        candidates = candidates.Where( c => !c.IsExplicitInterfaceMemberImplementation() );

        foreach ( var sourceItem in candidates )
        {
            if ( (isStatic != null && isStatic != sourceItem.IsStatic)
                 || (argumentCount != null && !expandParams && sourceItem.GetParameters().Length != argumentCount)
                 || (argumentCount != null && expandParams && sourceItem.GetParameters().Length > argumentCount + 1) )
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

            if ( sourceItem.GetParameters().Length > 0 )
            {
                for ( var parameterIndex = 0; parameterIndex < sourceItem.GetParameters().Length; parameterIndex++ )
                {
                    var parameter = sourceItem.GetParameters()[parameterIndex];

                    if ( parameter.IsParams && expandParams && match
                         && !(parameterIndex < argumentCount && argumentPredicate( payload, parameterIndex, parameter.Type, RefKind.Ref )) )
                    {
                        if ( parameterIndex != sourceItem.GetParameters().Length - 1 )
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
                if ( !tryMatchParams && argumentCount != sourceItem.GetParameters().Length )
                {
                    // Will not be matching params and parameter counts don't match.
                    continue;
                }

                yield return sourceItem;
            }
            else if ( tryMatchParams )
            {
                // Attempt to match C# params - all remaining parameter types should be assignable to the array element type.
                var parameterType = sourceItem.GetParameters()[sourceItem.GetParameters().Length - 1].Type;
                var elementType = GetParamsElementType( parameterType, compilation );

                if ( elementType == null )
                {
                    continue;
                }

                var paramsMatch = true;

                for ( var i = sourceItem.GetParameters().Length - 1; i < argumentCount; i++ )
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

    private static ITypeSymbol? GetParamsElementType( ITypeSymbol type, CompilationContext compilation )
    {
        // See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-13.0/params-collections#method-parameters.

        return type switch
        {
            IArrayTypeSymbol { Rank: 1 } arrayType => arrayType.ElementType,
            INamedTypeSymbol namedType when namedType.GetFullName() is "System.Span" or "System.ReadOnlySpan"
                => namedType.TypeArguments[0],
            INamedTypeSymbol namedType when namedType.GetAttributes().Any( a => a.AttributeClass.GetFullName() == "System.Runtime.CompilerServices.CollectionBuilderAttribute" )
                => GetIterationType( namedType, compilation ),
            INamedTypeSymbol { TypeKind: TypeKind.Struct or TypeKind.Class } namedType when
                namedType.AllInterfaces.Any( i => i.GetFullName() == "System.Collections.Generic.IEnumerable" )
                => GetIterationType( namedType, compilation ),
            INamedTypeSymbol { TypeKind: TypeKind.Interface } namedType when
                namedType.GetFullName() is "System.Collections.Generic.IEnumerable" or "System.Collections.Generic.IReadOnlyCollection"
                    or "System.Collections.Generic.IReadOnlyList" or "System.Collections.Generic.ICollection" or "System.Collections.Generic.IList"
                => namedType.TypeArguments[0],
            _ => null
        };
    }

    private static ITypeSymbol? GetIterationType( INamedTypeSymbol collectionType, CompilationContext compilation )
    {
        // This determination of iteration type is specific to collection expressions and params collections.
        // The iteration type for foreach would also need to consider extension method GetEnumerator.

        // This is based on https://github.com/dotnet/csharpstandard/blob/draft-v9/standard/statements.md#1395-the-foreach-statement,
        // but see https://github.com/dotnet/csharpstandard/issues/1188 (we follow the compiler, not the spec, where the two disagree).

        var getEnumeratorMethods =
            collectionType.GetMethodsOfCompatibleSignature( compilation, nameof(IEnumerable.GetEnumerator), argumentTypes: [], compilation, isStatic: false )
                .Where( m => m.DeclaredAccessibility == Accessibility.Public )
                .ToArray();

        if ( getEnumeratorMethods is [var getEnumeratorMethod] )
        {
            var enumeratorType = getEnumeratorMethod.ReturnType as INamedTypeSymbol;

            return enumeratorType?.GetMembers( "Current" ).OfType<IPropertySymbol>().SingleOrDefault()?.Type;
        }
        else
        {
            var genericEnumerableInterfaces = collectionType.AllInterfaces
                .Where( i => i.GetFullName() == "System.Collections.Generic.IEnumerable" )
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

            var hasNonGenericEnumerableInterface = collectionType.AllInterfaces
                .Any( i => i.GetFullName() == "System.Collections.IEnumerable" );

            if ( hasNonGenericEnumerableInterface )
            {
                return compilation.Compilation.ObjectType;
            }

            return null;
        }
    }
}