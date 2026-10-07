// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Services;

/// <summary>
/// Creates an <see cref="IObjectReader"/> that reads the names and values of an object, as Metalama reads the <c>args</c> and <c>tags</c> of an
/// advice. Extensions use it to accept arguments in the same forms.
/// </summary>
/// <remarks>
/// Get the service from <see cref="Project.IProject.ServiceProvider"/>, for instance <c>declaration.Compilation.Project.ServiceProvider</c>.
/// </remarks>
public interface IObjectReaderFactory : IProjectService
{
    /// <summary>
    /// Returns an <see cref="IObjectReader"/> for an object.
    /// </summary>
    /// <param name="instance">The object: an <see cref="IObjectReader"/>, which is returned as it is, an
    /// <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/> of names and values, or any other object, whose public instance
    /// properties and fields are read, usually an anonymous object. <c>null</c> gives an empty reader.</param>
    IObjectReader GetReader( object? instance );
}
