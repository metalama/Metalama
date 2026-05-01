// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Pipeline;

[ComImport]
[Guid( "2C0990E9-CDA8-453F-8614-7FA3F76EE1EE" )]
public interface ITransitiveCompilationResult
{
    bool IsSuccessful { get; }

    bool IsPipelinePaused { get; }

    byte[]? Manifest { get; }

    Diagnostic[] Diagnostics { get; }
}