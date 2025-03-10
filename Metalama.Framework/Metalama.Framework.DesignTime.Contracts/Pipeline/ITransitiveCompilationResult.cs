// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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