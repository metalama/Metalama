// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Options;

/// <summary>
/// Provides read-only access to hierarchical options for a given declaration.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a low-level mechanism to query options for any declaration. In typical usage, options are accessed through
/// the <see cref="DeclarationExtensions.Enhancements{T}"/>.<see cref="DeclarationEnhancements{T}.GetOptions{TOptions}"/> extension method,
/// which is more convenient.
/// </para>
/// <para>
/// The options returned by <see cref="GetOptions"/> are the result of merging all options that apply to the declaration along various
/// inheritance axes (base types, containing types, namespaces, and the project itself). See <see cref="ApplyChangesAxis"/> for details
/// about the different axes.
/// </para>
/// </remarks>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso cref="IHierarchicalOptions{T}"/>
/// <seealso cref="DeclarationEnhancements{T}.GetOptions{TOptions}"/>
/// <seealso href="@exposing-options"/>
/// <seealso href="@configuration-custom-merge"/>
[CompileTime]
public interface IHierarchicalOptionsManager
{
    /// <summary>
    /// Gets the merged hierarchical options that apply to a specific declaration.
    /// </summary>
    /// <param name="declaration">The declaration for which to retrieve options.</param>
    /// <param name="optionsType">The type of options to retrieve, which must implement <see cref="IHierarchicalOptions"/>.</param>
    /// <returns>The merged options for the declaration, or <c>null</c> if no options of the specified type apply to the declaration.</returns>
    IHierarchicalOptions? GetOptions( IDeclaration declaration, Type optionsType );
}