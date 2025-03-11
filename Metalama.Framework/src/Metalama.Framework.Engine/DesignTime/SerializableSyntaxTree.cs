// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.DesignTime;

[JsonObject]
public class SerializableSyntaxTree
{
    public string Text { get; }

    public ImmutableArray<SerializableAnnotation> Annotations { get; }

    public string FilePath { get; }

    [JsonConstructor]
    public SerializableSyntaxTree(
        string filePath,
        string text,
        ImmutableArray<SerializableAnnotation> annotations )
    {
        this.FilePath = filePath;
        this.Text = text;
        this.Annotations = annotations;
    }
}