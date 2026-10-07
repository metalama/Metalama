// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.ExtensionPoints;

/// <summary>
/// The object that the test extension exposes to the templates of the methods that it declares, when
/// <see cref="TestTemplateRedirectionOptions.WithMetaExtension"/> is set.
/// </summary>
[CompileTime]
public sealed class TestMetaExtension : IMetaExtension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestMetaExtension"/> class.
    /// </summary>
    public TestMetaExtension( string description )
    {
        this.Description = description;
    }

    /// <summary>
    /// Gets a description of the call site for which the method was declared.
    /// </summary>
    public string Description { get; }
}
