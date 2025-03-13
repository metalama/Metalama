// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Pipeline;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.VersionNeutral;

internal sealed class TransitiveCompilationResult : ITransitiveCompilationResult
{
    public bool IsSuccessful { get; }

    public bool IsPipelinePaused { get; }

    public byte[]? Manifest { get; }

    public Diagnostic[] Diagnostics { get; }

    private TransitiveCompilationResult( bool isSuccessful, bool isPipelinePaused, byte[]? manifest, Diagnostic[] diagnostics )
    {
        this.IsSuccessful = isSuccessful;
        this.IsPipelinePaused = isPipelinePaused;
        this.Manifest = manifest;
        this.Diagnostics = diagnostics;
    }

    public static TransitiveCompilationResult Success( bool isPipelinePaused, byte[] manifest )
        => new( true, isPipelinePaused, manifest, Array.Empty<Diagnostic>() );

    public static TransitiveCompilationResult Failed( Diagnostic[] diagnostics ) => new( false, false, null, diagnostics );
}