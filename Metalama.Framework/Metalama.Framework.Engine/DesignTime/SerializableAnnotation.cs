// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;

namespace Metalama.Framework.Engine.DesignTime;

[JsonObject]
public readonly struct SerializableAnnotation
{
    public int SpanStart { get; }

    public int SpanLength { get; }

    public SerializableAnnotationKind Kind { get; }

    public SerializableAnnotationTargetKind TargetKind { get; }

    public string? Data { get; }

    public SerializableAnnotation( SerializableAnnotationTargetKind targetKind, int spanStart, int spanLength, SerializableAnnotationKind kind, string? data )
    {
        this.TargetKind = targetKind;
        this.SpanStart = spanStart;
        this.SpanLength = spanLength;
        this.Kind = kind;
        this.Data = data;
    }
}