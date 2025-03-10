// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics;

public sealed class ConstantDiagnosticExtensionPolicy : IDiagnosticExtensionPolicy
{
    public static ConstantDiagnosticExtensionPolicy None { get; } = new( DiagnosticExtensionHandling.None );

    public static ConstantDiagnosticExtensionPolicy PropertiesOnly { get; } = new( DiagnosticExtensionHandling.PropertiesOnly );

    private readonly DiagnosticExtensionHandling _handling;

    private ConstantDiagnosticExtensionPolicy( DiagnosticExtensionHandling handling )
    {
        this._handling = handling;
    }

    public DiagnosticExtensionHandling GetHandling( IDiagnosticDefinition diagnosticDefinition, Location? location, IDiagnosticExtension extension )
        => this._handling;
}