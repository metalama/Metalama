// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed record ResolvedAttributeRef( ImmutableArray<AttributeData> Attributes, ISymbol ParentSymbol, RefTargetKind ParentRefTargetKind )
{
    public static ResolvedAttributeRef Invalid { get; } = new( default, null!, default );
}