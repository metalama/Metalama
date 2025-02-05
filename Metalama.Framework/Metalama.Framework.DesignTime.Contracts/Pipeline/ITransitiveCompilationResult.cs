// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Pipeline;

[ComImport]
[Guid( "CDA98261-4BAD-4117-8054-49390BCBF4E6" )]
public interface ITransitiveCompilationResult
{
    bool IsSuccessful { get; }

    bool IsPipelinePaused { get; }

    byte[]? Manifest { get; }

    Diagnostic[] Diagnostics { get; }
}