// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.SourceGeneration;

/// <summary>
/// Names of the per-project touch-marker class. The class lives in a <c>.cs</c> file under <c>obj/</c>
/// that is included as a regular <c>&lt;Compile&gt;</c> item. The design-time pipeline overwrites the
/// <see cref="MarkerFieldName"/> GUID on every run; Roslyn re-runs the source generator whenever a
/// <c>&lt;Compile&gt;</c> file changes. The class is marked <c>[Obsolete(..., error: true)]</c> so user
/// code cannot reference it — that, plus the <c>internal</c> accessibility, means cross-project name
/// collisions via <c>[InternalsVisibleTo]</c> are harmless (any reference would already be a compile
/// error).
/// </summary>
internal static class TouchFileHelper
{
    public const string MarkerNamespace = "Metalama.Internal";
    public const string MarkerTypeName = "MetalamaSourceGeneratorMarker";
    public const string MarkerFieldName = "TouchId";
    public const string MarkerFullTypeName = MarkerNamespace + "." + MarkerTypeName;
}
