// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code;

/// <summary>
/// Provides extension methods to the <see cref="IMethodCollection"/> interface.
/// </summary>
[CompileTime]
public static class MethodCollectionExtensions
{
    /// <summary>
    /// Gets the list of methods with signatures compatible with specified constraints.
    /// </summary>
    /// <param name="methods">A collection of methods.</param>
    /// <param name="name">Name of the method.</param>
    /// <param name="argumentTypes">Constraint on reflection types of arguments. <c>Null</c>items in the list signify any type.</param>
    /// <param name="isStatic">Constraint on staticity of the method.</param>
    /// <returns>Enumeration of methods matching specified constraints.</returns>
    public static IEnumerable<IMethod> OfCompatibleSignature(
        this IMethodCollection methods,
        string name,
        IReadOnlyList<Type?>? argumentTypes,
        bool? isStatic = false )
    {
        return methods.OfCompatibleSignature(
            (argumentTypes, (ICompilationInternal) methods.DeclaringType.Compilation),
            name,
            argumentTypes?.Count,
            GetParameter,
            isStatic );

        static (IType? Type, RefKind? RefKind) GetParameter( (IReadOnlyList<Type?>? ArgumentTypes, ICompilationInternal Compilation) context, int index )
            => context.ArgumentTypes?[index] != null
                ? (context.Compilation.Factory.GetTypeByReflectionType( context.ArgumentTypes[index]! ), null)
                : (null, null);
    }

    /// <summary>
    /// Gets the list of methods with signatures compatible with specified constraints.
    /// </summary>
    /// <param name="methods">A collection of methods.</param>
    /// <param name="name">Name of the method.</param>
    /// <param name="argumentTypes">Constraint on types of arguments. <c>Null</c>items in the list signify any type.</param>
    /// <param name="refKinds">Constraint on reference kinds of arguments. <c>Null</c>items in the list signify any reference kind.</param>
    /// <param name="isStatic">Constraint on staticity of the method.</param>
    /// <returns>Enumeration of methods matching specified constraints.</returns>
    public static IEnumerable<IMethod> OfCompatibleSignature(
        this IMethodCollection methods,
        string name,
        IReadOnlyList<IType?>? argumentTypes,
        IReadOnlyList<RefKind?>? refKinds = null,
        bool? isStatic = false )
    {
        return methods.OfCompatibleSignature(
            (argumentTypes, refKinds),
            name,
            argumentTypes?.Count,
            GetParameter,
            isStatic );

        static (IType? Type, RefKind? RefKind) GetParameter( (IReadOnlyList<IType?>? ArgumentTypes, IReadOnlyList<RefKind?>? RefKinds) context, int index )
            => (context.ArgumentTypes?[index], context.RefKinds?[index]);
    }

    /// <summary>
    /// Gets a method that exactly matches the specified signature.
    /// </summary>
    /// <param name="methods">A collection of methods.</param>
    /// <param name="name">Name of the method.</param>
    /// <param name="parameterTypes">List of parameter types.</param>
    /// <param name="refKinds">List of parameter reference kinds, or <c>null</c> if all parameters should be by-value.</param>
    /// <param name="isStatic">Staticity of the method.</param>
    /// <returns>A <see cref="IMethod"/> that matches the given signature.</returns>
    public static IMethod? OfExactSignature(
        this IMethodCollection methods,
        string name,
        IReadOnlyList<IType> parameterTypes,
        IReadOnlyList<RefKind>? refKinds = null,
        bool? isStatic = null )
    {
        return methods.OfExactSignature(
            (parameterTypes, refKinds),
            name,
            parameterTypes.Count,
            GetParameter,
            isStatic );

        static (IType Type, RefKind RefKind) GetParameter( (IReadOnlyList<IType> ParameterTypes, IReadOnlyList<RefKind>? RefKinds) context, int index )
            => (context.ParameterTypes[index], context.RefKinds?[index] ?? RefKind.None);
    }

    /// <summary>
    /// Gets a method that matches the specified signature using the specified <see cref="ConversionKind"/> for parameter type comparison.
    /// Unlike <see cref="OfExactSignature(IMethodCollection, string, IReadOnlyList{IType}, IReadOnlyList{RefKind}?, bool?)"/> which requires identical types,
    /// this overload allows specifying a <see cref="ConversionKind"/> such as <see cref="ConversionKind.Default"/> for implicit conversions.
    /// </summary>
    /// <param name="methods">A collection of methods.</param>
    /// <param name="name">Name of the method.</param>
    /// <param name="parameterTypes">List of parameter types.</param>
    /// <param name="refKinds">List of parameter reference kinds, or <c>null</c> if all parameters should be by-value.</param>
    /// <param name="isStatic">Staticity of the method.</param>
    /// <param name="conversionKind">The <see cref="ConversionKind"/> to use for parameter type comparison.</param>
    /// <returns>A <see cref="IMethod"/> that matches the given signature.</returns>
    public static IMethod? OfCompatibleSignature(
        this IMethodCollection methods,
        string name,
        IReadOnlyList<IType> parameterTypes,
        IReadOnlyList<RefKind>? refKinds,
        bool? isStatic,
        ConversionKind conversionKind )
    {
        return methods.OfCompatibleSignature(
            (parameterTypes, refKinds),
            name,
            parameterTypes.Count,
            GetParameter,
            isStatic,
            conversionKind );

        static (IType Type, RefKind RefKind) GetParameter( (IReadOnlyList<IType> ParameterTypes, IReadOnlyList<RefKind>? RefKinds) context, int index )
            => (context.ParameterTypes[index], context.RefKinds?[index] ?? RefKind.None);
    }

    /// Gets a method that exactly matches the specified signature, including the number of generic type parameters.
    /// </summary>
    /// <param name="methods">A collection of methods.</param>
    /// <param name="name">Name of the method.</param>
    /// <param name="typeParameterCount">Required number of generic type parameters.</param>
    /// <param name="parameterTypes">List of parameter types.</param>
    /// <param name="refKinds">List of parameter reference kinds, or <c>null</c> if all parameters should be by-value.</param>
    /// <param name="isStatic">Staticity of the method.</param>
    /// <returns>A <see cref="IMethod"/> that matches the given signature, or <c>null</c> if no match is found.</returns>
    public static IMethod? OfExactSignature(
        this IMethodCollection methods,
        string name,
        int typeParameterCount,
        IReadOnlyList<IType> parameterTypes,
        IReadOnlyList<RefKind>? refKinds = null,
        bool? isStatic = null )
    {
        return methods.OfExactSignature(
            (parameterTypes, refKinds),
            name,
            parameterTypes.Count,
            GetParameter,
            isStatic,
            typeParameterCount );

        static (IType Type, RefKind RefKind) GetParameter( (IReadOnlyList<IType> ParameterTypes, IReadOnlyList<RefKind>? RefKinds) context, int index )
            => (context.ParameterTypes[index], context.RefKinds?[index] ?? RefKind.None);
    }

    /// <summary>
    /// Gets a method that exactly matches the specified signature given using the <c>System.Reflection</c> API.
    /// By-ref reflection types (<see cref="Type.IsByRef"/> == <see langword="true"/>) do not match any parameter.
    /// Non-by-ref reflection types match parameters with <see cref="RefKind.None"/> or <see cref="RefKind.In"/>.
    /// </summary>
    /// <param name="methods">A collection of methods.</param>
    /// <param name="name">Name of the method.</param>
    /// <param name="parameterTypes">List of parameter types as reflection <see cref="Type"/> objects.</param>
    /// <param name="isStatic">Staticity of the method.</param>
    /// <returns>A <see cref="IMethod"/> that matches the given signature.</returns>
    public static IMethod? OfExactSignature(
        this IMethodCollection methods,
        string name,
        IReadOnlyList<Type> parameterTypes,
        bool? isStatic = null )
    {
        // By-ref reflection types are not supported by this overload.
        for ( var i = 0; i < parameterTypes.Count; i++ )
        {
            if ( parameterTypes[i].IsByRef )
            {
                return null;
            }
        }

        var compilation = (ICompilationInternal) methods.DeclaringType.Compilation;

        foreach ( var method in methods.OfName( name ) )
        {
            if ( method.IsExplicitInterfaceImplementation )
            {
                continue;
            }

            if ( isStatic != null && method.IsStatic != isStatic )
            {
                continue;
            }

            if ( method.Parameters.Count != parameterTypes.Count )
            {
                continue;
            }

            var match = true;

            for ( var i = 0; i < parameterTypes.Count; i++ )
            {
                var parameter = method.Parameters[i];

                // Non-by-ref reflection types match only plain and 'in' parameters.
                if ( parameter.RefKind != RefKind.None && parameter.RefKind != RefKind.In )
                {
                    match = false;

                    break;
                }

                var metalmaType = compilation.Factory.GetTypeByReflectionType( parameterTypes[i] );

                if ( !compilation.Comparers.Default.IsConvertibleTo( metalmaType, parameter.Type, ConversionKind.Identical ) )
                {
                    match = false;

                    break;
                }
            }

            if ( match )
            {
                return method;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a method that exactly matches the signature of the specified method.
    /// </summary>
    /// <param name="methods">A collection of methods.</param>
    /// <param name="signatureTemplate">Method signature of which to should be considered.</param>
    /// <param name="matchIsStatic">Value indicating whether the staticity of the method should be matched.</param>
    /// <returns>A <see cref="IMethod"/> that matches the given signature.</returns>
    public static IMethod? OfExactSignature( this IMethodCollection methods, IMethod signatureTemplate, bool matchIsStatic = true )
    {
        return methods.OfExactSignature(
            signatureTemplate,
            signatureTemplate.Name,
            signatureTemplate.Parameters.Count,
            GetParameter,
            matchIsStatic ? signatureTemplate.IsStatic : null,
            signatureTemplate.TypeParameters.Count );

        static (IType Type, RefKind RefKind) GetParameter( IMethod context, int index ) => (context.Parameters[index].Type, context.Parameters[index].RefKind);
    }

    /// <summary>
    /// Gets an indexer that exactly matches the specified signature.
    /// </summary>
    /// <param name="indexers">A collection of indexers.</param>
    /// <param name="parameterTypes">List of parameter types.</param>
    /// <param name="refKinds">List of parameter reference kinds, or <c>null</c> if all parameters should be by-value.</param>
    /// <returns>An <see cref="IIndexer"/> that matches the given signature.</returns>
    public static IIndexer? OfExactSignature(
        this IIndexerCollection indexers,
        IReadOnlyList<IType> parameterTypes,
        IReadOnlyList<RefKind>? refKinds = null )
    {
        return indexers.OfExactSignature(
            (parameterTypes, refKinds),
            null,
            parameterTypes.Count,
            GetParameter,
            null );

        static (IType Type, RefKind RefKind) GetParameter( (IReadOnlyList<IType> ParameterTypes, IReadOnlyList<RefKind>? RefKinds) context, int index )
            => (context.ParameterTypes[index], context.RefKinds?[index] ?? RefKind.None);
    }

    /// <summary>
    /// Gets an indexer that exactly matches the signature of the specified method.
    /// </summary>
    /// <param name="indexers">A collection of indexers.</param>
    /// <param name="signatureTemplate">Indexer signature of which to should be considered.</param>
    /// <returns>A <see cref="IMethod"/> that matches the given signature.</returns>
    public static IIndexer? OfExactSignature( this IIndexerCollection indexers, IIndexer signatureTemplate )
    {
        return indexers.OfExactSignature(
            signatureTemplate,
            null,
            signatureTemplate.Parameters.Count,
            GetParameter,
            null );

        static (IType Type, RefKind RefKind) GetParameter( IIndexer context, int index ) => (context.Parameters[index].Type, context.Parameters[index].RefKind);
    }

    /// <summary>
    /// Gets the list of methods of a given <see cref="MethodKind"/> (such as <see cref="MethodKind.Operator"/> or <see cref="MethodKind.Default"/>.
    /// </summary>
    public static IEnumerable<IMethod> OfKind( this IMethodCollection methods, MethodKind kind ) => methods.Where( m => m.MethodKind == kind );
}