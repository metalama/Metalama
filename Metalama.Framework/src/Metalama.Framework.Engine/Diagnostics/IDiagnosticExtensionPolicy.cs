// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Diagnostics;

public interface IDiagnosticExtensionPolicy : IProjectService
{
    [PublicAPI]
    DiagnosticExtensionHandling GetHandling(
        IDiagnosticDefinition diagnosticDefinition,
        Location? location,
        IDiagnosticExtension extension );
}