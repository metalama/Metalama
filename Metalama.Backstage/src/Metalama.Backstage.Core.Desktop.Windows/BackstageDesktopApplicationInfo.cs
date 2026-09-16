// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using System.Reflection;

namespace Metalama.Backstage.Desktop.Windows;

/// <summary>
/// The description of the desktop notifier process of a product. The executable of the product creates it with its
/// own profile and passes it to the initialization options of the Backstage services.
/// </summary>
[PublicAPI]
public sealed class BackstageDesktopApplicationInfo : ApplicationInfoBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackstageDesktopApplicationInfo"/> class.
    /// </summary>
    /// <param name="metadataAssembly">The assembly whose metadata gives the version of the notifier, normally the executable.</param>
    /// <param name="productProfile">The profile of the product.</param>
    /// <param name="name">The name of the notifier, for instance <c>Metalama.Backstage.Desktop.Windows</c>.</param>
    public BackstageDesktopApplicationInfo( Assembly metadataAssembly, ProductProfile productProfile, string name )
        : base( metadataAssembly, productProfile )
    {
        this.Name = name;
    }

    /// <inheritdoc />
    public override string Name { get; }

    /// <inheritdoc />
    public override ProcessKind ProcessKind => ProcessKind.BackstageDesktopWindows;
}
