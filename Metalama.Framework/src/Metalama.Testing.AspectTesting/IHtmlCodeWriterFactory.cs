// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// Factory for creating <see cref="IHtmlCodeWriter"/> instances.
/// When no implementation is available (i.e., when the <c>Metalama.Extensions.HtmlWriter</c> package is not referenced),
/// HTML output features are disabled and tests continue to work normally.
/// </summary>
/// <remarks>
/// <para>
/// To enable HTML output support, add a reference to the <c>Metalama.Extensions.HtmlWriter</c> package.
/// </para>
/// </remarks>
[PublicAPI]
public interface IHtmlCodeWriterFactory
{
    /// <summary>
    /// Creates an <see cref="IHtmlCodeWriter"/> instance using the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The project service provider.</param>
    /// <returns>An <see cref="IHtmlCodeWriter"/> instance.</returns>
    IHtmlCodeWriter Create( in ProjectServiceProvider serviceProvider );
}
