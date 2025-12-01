// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Extension methods for resolving <see cref="IRef{T}"/> references to their target declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides the primary methods for resolving references to declarations. References are used because
    /// <see cref="IDeclaration"/> objects are bound to a specific <see cref="ICompilation"/>, which changes at each
    /// step of the aspect pipeline. References provide a stable way to identify the same declaration across these
    /// compilation versions.
    /// </para>
    /// <para>
    /// <b>Key methods:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="GetTarget{T}(IRef{T})"/>: Resolves a reference using the current execution context.</description>
    /// </item>
    /// <item>
    /// <description><see cref="GetTarget{T}(IRef{T},ICompilation,IGenericContext?)"/>: Resolves a reference in a specific compilation.</description>
    /// </item>
    /// <item>
    /// <description><see cref="GetTargetOrNull{T}(IRef{T})"/>: Like GetTarget but returns <c>null</c> instead of throwing.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRef{T}"/>
    /// <seealso cref="IRef"/>
    /// <seealso cref="IDeclaration.ToRef"/>
    /// <seealso href="@aspect-serialization"/>
    [PublicAPI]
    [CompileTime]
    public static class RefExtensions
    {
        /// <summary>
        /// Resolves a reference to its target declaration within a specific compilation.
        /// </summary>
        /// <typeparam name="T">The type of compilation element, such as <see cref="IMethod"/>, <see cref="IProperty"/>, or <see cref="INamedType"/>.</typeparam>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="compilation">The compilation in which to resolve the reference. This may be a different compilation version
        /// than the one from which the reference was obtained.</param>
        /// <param name="genericContext">The optional generic context for resolving references within generic types or methods.
        /// Pass this when the declaration needs to be resolved in the context of a specific generic instantiation.</param>
        /// <returns>The resolved declaration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the reference cannot be resolved in the specified compilation.</exception>
        /// <remarks>
        /// <para>
        /// To resolve a reference using the current execution context (e.g., within a template or <c>BuildAspect</c>),
        /// use <see cref="GetTarget{T}(IRef{T})"/> instead.
        /// </para>
        /// </remarks>
        public static T GetTarget<T>( this IRef<T> reference, ICompilation compilation, IGenericContext? genericContext = null )
            where T : class, ICompilationElement
            => (T) reference.GetTargetInterface( compilation, typeof(T), genericContext, true )!;

        /// <summary>
        /// Resolves a non-generic reference to its target declaration within a specific compilation.
        /// </summary>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="compilation">The compilation in which to resolve the reference.</param>
        /// <param name="genericContext">The optional generic context for resolving references within generic types or methods.</param>
        /// <param name="interfaceType">The optional interface type to return. When <c>null</c>, the default interface for the
        /// declaration type is used.</param>
        /// <returns>The resolved declaration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the reference cannot be resolved in the specified compilation.</exception>
        public static ICompilationElement GetTarget(
            this IRef reference,
            ICompilation compilation,
            IGenericContext? genericContext = null,
            Type? interfaceType = null )
            => reference.GetTargetInterface( compilation, interfaceType, genericContext, true )!;

        /// <summary>
        /// Resolves a reference to its target declaration within a specific compilation, returning <c>null</c> if the reference
        /// cannot be resolved.
        /// </summary>
        /// <typeparam name="T">The type of compilation element, such as <see cref="IMethod"/>, <see cref="IProperty"/>, or <see cref="INamedType"/>.</typeparam>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="compilation">The compilation in which to resolve the reference. This may be a different compilation version
        /// than the one from which the reference was obtained.</param>
        /// <param name="genericContext">The optional generic context for resolving references within generic types or methods.</param>
        /// <returns>The resolved declaration, or <c>null</c> if the reference cannot be resolved (e.g., if the declaration
        /// was removed from the compilation).</returns>
        /// <remarks>
        /// <para>
        /// To resolve a reference using the current execution context, use <see cref="GetTargetOrNull{T}(IRef{T})"/> instead.
        /// </para>
        /// </remarks>
        public static T? GetTargetOrNull<T>( this IRef<T> reference, ICompilation compilation, IGenericContext? genericContext = null )
            where T : class, ICompilationElement
            => (T?) reference.GetTargetInterface( compilation, typeof(T), genericContext );

        /// <summary>
        /// Resolves a non-generic reference to its target declaration within a specific compilation, returning <c>null</c>
        /// if the reference cannot be resolved.
        /// </summary>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="compilation">The compilation in which to resolve the reference.</param>
        /// <param name="genericContext">The optional generic context for resolving references within generic types or methods.</param>
        /// <param name="interfaceType">The optional interface type to return. When <c>null</c>, the default interface for the
        /// declaration type is used.</param>
        /// <returns>The resolved declaration, or <c>null</c> if the reference cannot be resolved.</returns>
        public static ICompilationElement? GetTargetOrNull(
            this IRef reference,
            ICompilation compilation,
            IGenericContext? genericContext = null,
            Type? interfaceType = null )
            => reference.GetTargetInterface( compilation, interfaceType, genericContext );

        [Obsolete( "Use the overload that accepts a RefComparison" )]
        public static bool Equals( this IRef a, IRef? b, bool includeNullability )
            => a.Equals( b, includeNullability ? RefComparison.IncludeNullability : RefComparison.Default );

        /// <summary>
        /// Resolves a reference to its target declaration using the current Metalama execution context.
        /// </summary>
        /// <typeparam name="T">The type of compilation element, such as <see cref="IMethod"/>, <see cref="IProperty"/>, or <see cref="INamedType"/>.</typeparam>
        /// <param name="reference">The reference to resolve.</param>
        /// <returns>The resolved declaration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the reference cannot be resolved or when called outside
        /// of a Metalama execution context (e.g., from a template, <c>BuildAspect</c>, or fabric).</exception>
        /// <remarks>
        /// <para>
        /// This is the most common way to resolve a reference. It uses <see cref="MetalamaExecutionContext.Current"/>
        /// to obtain the current compilation. Use this method when resolving references during aspect execution.
        /// </para>
        /// <para>
        /// For resolving references in a specific compilation (e.g., when comparing declarations across compilation versions),
        /// use <see cref="GetTarget{T}(IRef{T},ICompilation,IGenericContext?)"/> instead.
        /// </para>
        /// </remarks>
        public static T GetTarget<T>( this IRef<T> reference )
            where T : class, ICompilationElement
            => reference.GetTarget( MetalamaExecutionContext.Current.Compilation );

        /// <summary>
        /// Resolves a reference to its target declaration using the current Metalama execution context, returning <c>null</c>
        /// if the reference cannot be resolved.
        /// </summary>
        /// <typeparam name="T">The type of compilation element, such as <see cref="IMethod"/>, <see cref="IProperty"/>, or <see cref="INamedType"/>.</typeparam>
        /// <param name="reference">The reference to resolve.</param>
        /// <returns>The resolved declaration, or <c>null</c> if the reference cannot be resolved.</returns>
        /// <remarks>
        /// <para>
        /// This method uses <see cref="MetalamaExecutionContext.Current"/> to obtain the current compilation.
        /// It returns <c>null</c> instead of throwing when the reference cannot be resolved.
        /// </para>
        /// </remarks>
        public static T? GetTargetOrNull<T>( this IRef<T> reference )
            where T : class, ICompilationElement
            => reference.GetTargetOrNull( MetalamaExecutionContext.Current.Compilation );
    }
}