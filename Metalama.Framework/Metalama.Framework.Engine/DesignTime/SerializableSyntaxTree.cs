// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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