// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// A segment of a <see cref="RichTextNode"/>: either literal text or an argument.
/// </summary>
internal sealed record RichTextSegment( string? Text, ResultNode? Argument );
