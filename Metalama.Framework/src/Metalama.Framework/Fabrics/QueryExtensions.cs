// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Fabrics;

/// <summary>
/// Extends the <see cref="IQuery{TDeclaration}"/> interface with some utility methods.
/// </summary>
[CompileTime]
[PublicAPI]
public static class QueryExtensions
{
    /// <summary>
    /// Selects a reference assembly in the current compilation given its assembly name.
    /// </summary>
    /// <param name="receiver">A query selecting the compilation.</param>
    /// <param name="assemblyName">The name of the referenced assembly to select.</param>
    /// <returns>A query selecting the referenced assembly.</returns>
    public static IQuery<IAssembly> SelectReferencedAssembly( this IQuery<ICompilation> receiver, string assemblyName )
        => receiver.SelectMany( c => c.ReferencedAssemblies.OfName( assemblyName ) );

    /// <summary>
    /// Selects all custom attributes of a given type in the current compilation. This generic overloads constructs the attribute
    /// and accepts an optional predicate to filter the attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to search for. It must be a compile-time or run-time-or-compile-time type.</typeparam>
    /// <param name="receiver">A query selecting the compilation.</param>
    /// <param name="predicate">An optional predicate to filter attributes based on their constructed values.</param>
    /// <param name="includeDerivedTypes">Whether to include attributes whose type derives from <typeparamref name="TAttribute"/>.</param>
    /// <returns>A query selecting declarations that have the specified attribute.</returns>
    public static IQuery<IDeclaration> SelectDeclarationsWithAttribute<TAttribute>(
        this IQuery<ICompilation> receiver,
        Func<TAttribute, bool>? predicate = null,
        bool includeDerivedTypes = true )
        => receiver.SelectMany( c => c.GetDeclarationsWithAttribute( predicate, includeDerivedTypes ) );

    /// <summary>
    /// Selects all custom attributes of a given type in the current compilation. This overloads
    /// accepts an optional predicate to filter the attribute.
    /// </summary>
    /// <param name="receiver">A query selecting the compilation.</param>
    /// <param name="attributeType">The attribute type to search for.</param>
    /// <param name="predicate">An optional predicate to filter attributes based on their <see cref="IAttribute"/> representation.</param>
    /// <param name="includeDerivedTypes">Whether to include attributes whose type derives from <paramref name="attributeType"/>.</param>
    /// <returns>A query selecting declarations that have the specified attribute.</returns>
    public static IQuery<IDeclaration> SelectDeclarationsWithAttribute(
        this IQuery<ICompilation> receiver,
        Type attributeType,
        Func<IAttribute, bool>? predicate = null,
        bool includeDerivedTypes = true )
        => receiver.SelectMany( c => c.GetDeclarationsWithAttribute( attributeType, predicate, includeDerivedTypes ) );

    /// <summary>
    /// Selects an <see cref="INamedType"/> in the current compilation or in a reference assembly given its reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="receiver">A query selecting the compilation.</param>
    /// <param name="type">The reflection type to select.</param>
    /// <returns>A query selecting the named type corresponding to the reflection type.</returns>
    public static IQuery<INamedType> SelectReflectionType( this IQuery<ICompilation> receiver, Type type )
        => receiver.Select( c => (INamedType) ((ICompilationInternal) c).Factory.GetTypeByReflectionType( type ) );

    /// <summary>
    /// Selects several <see cref="INamedType"/> in the current compilation or in a reference assembly given their reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="receiver">A query selecting the compilation.</param>
    /// <param name="types">The collection of reflection types to select.</param>
    /// <returns>A query selecting the named types corresponding to the reflection types.</returns>
    public static IQuery<INamedType> SelectReflectionTypes( this IQuery<ICompilation> receiver, IEnumerable<Type> types )
        => receiver.SelectMany( c => types.Select( t => (INamedType) ((ICompilationInternal) c).Factory.GetTypeByReflectionType( t ) ) );

    /// <summary>
    /// Selects several <see cref="INamedType"/> in the current compilation or in a reference assembly given their reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="receiver">A query selecting the compilation.</param>
    /// <param name="types">The array of reflection types to select.</param>
    /// <returns>A query selecting the named types corresponding to the reflection types.</returns>
    public static IQuery<INamedType> SelectReflectionTypes( this IQuery<ICompilation> receiver, params Type[] types )
        => receiver.SelectReflectionTypes( (IEnumerable<Type>) types );
}