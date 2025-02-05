// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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