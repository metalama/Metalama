// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Observability;

/// <summary>
/// When applied to the <c>OnChildPropertyChanged</c> and <c>OnObservablePropertyChanged</c> method, indicates that the specified expressions
/// are observed using these methods.
/// </summary>
[PublicAPI]
[AttributeUsage( AttributeTargets.Method )]
public sealed class ObservedExpressionsAttribute : Attribute
{
    // ReSharper disable once UnusedParameter.Local
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservedExpressionsAttribute"/> class.
    /// </summary>
    /// <param name="parentPropertyPaths">
    /// The parent property paths which the declaring class monitors for reference and child property changes,
    /// and for which  <c>OnChildPropertyChanged</c> or <c>OnObservablePropertyChanged</c>  will be called.
    /// </param>
    public ObservedExpressionsAttribute( params string[] parentPropertyPaths ) { }
}