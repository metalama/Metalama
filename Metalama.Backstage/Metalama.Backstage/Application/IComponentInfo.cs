// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Application;

/// <summary>
/// Exposes information about the components, or plug-ins, of an application. This information
/// is consumed by the licensing component. For instance, Metalama.Framework is a component of Metalama.Compiler.
/// </summary>
public interface IComponentInfo
{
    /// <summary>
    /// Gets the name of the author who published the component.
    /// </summary>
    string? Company { get; }

    /// <summary>
    /// Gets the name of the component.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a version of the component.
    /// </summary>
    string? PackageVersion { get; }

    Version? AssemblyVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the component is a pre-release.
    /// </summary>
    bool? IsPrerelease { get; }

    /// <summary>
    /// Gets a date of build of the component.
    /// </summary>
    DateTime? BuildDate { get; }
}