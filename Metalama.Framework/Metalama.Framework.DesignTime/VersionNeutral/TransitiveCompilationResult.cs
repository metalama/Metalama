// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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