// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Options;

/// <summary>
/// Context for the <see cref="IHierarchicalOptionsProvider"/>.<see cref="IHierarchicalOptionsProvider.GetOptions"/> method.
/// </summary>
/// <remarks>
/// <para>
/// This context is provided when Metalama calls <see cref="IHierarchicalOptionsProvider.GetOptions"/> on an aspect or custom attribute
/// to retrieve the options it provides. The context gives access to the target declaration and allows reporting diagnostics if the
/// aspect's configuration properties are invalid.
/// </para>
/// </remarks>
/// <seealso cref="IHierarchicalOptionsProvider"/>
/// <seealso href="@exposing-options"/>
[CompileTime]
public readonly struct OptionsProviderContext
{
    /// <summary>
    /// Gets the declaration for which options are being provided.
    /// </summary>
    /// <value>
    /// The declaration (such as a type, method, or property) to which the aspect or attribute is applied and for which
    /// options are being provided.
    /// </value>
    public IDeclaration TargetDeclaration { get; }

    /// <summary>
    /// Gets a service allowing to report diagnostics.
    /// </summary>
    /// <value>
    /// A <see cref="ScopedDiagnosticSink"/> that can be used to report errors, warnings, or information messages if the
    /// options configuration is invalid or problematic.
    /// </value>
    public ScopedDiagnosticSink Diagnostics { get; }

    internal OptionsProviderContext( IDeclaration targetDeclaration, in ScopedDiagnosticSink diagnostics )
    {
        this.TargetDeclaration = targetDeclaration;
        this.Diagnostics = diagnostics;
    }
}