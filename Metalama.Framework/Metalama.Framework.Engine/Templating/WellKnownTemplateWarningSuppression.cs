// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating;

public sealed record WellKnownTemplateWarningSuppression( string DiagnosticId, string? Justification, SymbolKind[] EligibleSymbolKinds )
{
    internal SuppressionDefinition Definition { get; } = new( DiagnosticId, Justification );

    internal bool RequiresBody { get; init; }

    internal bool AppliesToConstructor { get; init; }
}