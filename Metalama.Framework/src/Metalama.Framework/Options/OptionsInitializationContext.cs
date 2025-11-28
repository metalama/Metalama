// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Project;

namespace Metalama.Framework.Options;

/// <summary>
/// Context of the <see cref="IHierarchicalOptions.GetDefaultOptions"/> method.
/// </summary>
/// <remarks>
/// <para>
/// This context is provided when Metalama calls <see cref="IHierarchicalOptions.GetDefaultOptions"/> to retrieve project-level
/// default options. The context allows the options class to access project properties (such as MSBuild properties) and report
/// diagnostics if configuration values are invalid.
/// </para>
/// </remarks>
/// <seealso cref="IHierarchicalOptions.GetDefaultOptions"/>
/// <seealso href="@reading-msbuild-properties"/>
[CompileTime]
public sealed class OptionsInitializationContext
{
    /// <summary>
    /// Gets the current project.
    /// </summary>
    /// <value>
    /// The <see cref="IProject"/> for which default options are being initialized. This can be used to read MSBuild properties
    /// or other project-level configuration.
    /// </value>
    public IProject Project { get; }

    /// <summary>
    /// Gets a service allowing to report diagnostics.
    /// </summary>
    /// <value>
    /// A <see cref="ScopedDiagnosticSink"/> that can be used to report errors, warnings, or information messages if the
    /// default options configuration is invalid or problematic.
    /// </value>
    public ScopedDiagnosticSink Diagnostics { get; }

    internal OptionsInitializationContext( IProject project, ScopedDiagnosticSink diagnostics )
    {
        this.Project = project;
        this.Diagnostics = diagnostics;
    }
}