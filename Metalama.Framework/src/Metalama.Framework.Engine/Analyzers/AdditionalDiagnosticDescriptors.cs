// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;
using static Metalama.Framework.Diagnostics.Severity;

namespace Metalama.Framework.Engine.Analyzers;

internal static class AdditionalDiagnosticDescriptors
{
    // Reserved range: | 0120-0139 | Additional
    private const string _category = "Metalama.General";

    internal static readonly DiagnosticDefinition<(INamedTypeSymbol InterfaceName, string Assembly)>
        CannotImplementBecauseOfInternalImplementAttribute = new(
            "LAMA0120",
            Error,
            "The interface '{0}' cannot be implemented in this project. It can only be implemented in '{1}' or in a project to which '{1}' " +
            "grants access with the InternalsVisibleTo attribute.",
            "The interface cannot be implemented in this project.",
            _category );
}