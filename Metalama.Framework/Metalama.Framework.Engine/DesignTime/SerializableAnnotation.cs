// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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