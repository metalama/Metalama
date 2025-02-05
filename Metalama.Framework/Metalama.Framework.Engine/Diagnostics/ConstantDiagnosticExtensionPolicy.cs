// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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