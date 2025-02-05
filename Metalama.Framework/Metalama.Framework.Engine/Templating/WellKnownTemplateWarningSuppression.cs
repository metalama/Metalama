// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating;

public sealed record WellKnownTemplateWarningSuppression( string DiagnosticId, string? Justification, SymbolKind[] EligibleSymbolKinds )
{
    internal SuppressionDefinition Definition { get; } = new( DiagnosticId, Justification );

    internal bool RequiresBody { get; init; }

    internal bool AppliesToConstructor { get; init; }
}