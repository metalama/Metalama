// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Options;

/// <summary>
/// Extensions of the <see cref="IIncrementalObject"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide type-safe and null-safe wrappers around the <see cref="IIncrementalObject.ApplyChanges"/> method,
/// making it easier to work with incremental objects in a strongly-typed manner.
/// </para>
/// </remarks>
/// <seealso cref="IIncrementalObject"/>
/// <seealso href="@exposing-options"/>
[CompileTime]
public static class IncrementalObjectExtensions
{
    /// <summary>
    /// Invokes <see cref="IIncrementalObject.ApplyChanges"/> in a type- and nullable-safe way.
    /// </summary>
    /// <typeparam name="T">The type of incremental object.</typeparam>
    /// <param name="baseOptions">The base options object, or <c>null</c>.</param>
    /// <param name="overrideOptions">The override options object, or <c>null</c>.</param>
    /// <param name="context">The context for applying changes.</param>
    /// <returns>The result of applying <paramref name="overrideOptions"/> to <paramref name="baseOptions"/>, handling null values.</returns>
    public static T? ApplyChangesSafe<T>( this T? baseOptions, T? overrideOptions, in ApplyChangesContext context )
        where T : class, IIncrementalObject
    {
        if ( baseOptions == null )
        {
            return overrideOptions;
        }
        else if ( overrideOptions == null )
        {
            return baseOptions;
        }
        else
        {
            return (T) baseOptions.ApplyChanges( overrideOptions, context );
        }
    }

    /// <summary>
    /// Invokes <see cref="IIncrementalObject.ApplyChanges"/> in a type-safe way.
    /// </summary>
    /// <typeparam name="T">The type of incremental object.</typeparam>
    /// <param name="baseOptions">The base options object.</param>
    /// <param name="overrideOptions">The override options object.</param>
    /// <param name="context">The context for applying changes.</param>
    /// <returns>The result of applying <paramref name="overrideOptions"/> to <paramref name="baseOptions"/>.</returns>
    public static T ApplyChanges<T>( this T baseOptions, T overrideOptions, in ApplyChangesContext context )
        where T : class, IIncrementalObject
        => (T) baseOptions.ApplyChanges( overrideOptions, context );
}