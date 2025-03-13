// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#nullable disable

using System.Collections.Generic;

// ReSharper disable MemberCanBeInternal
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    public sealed class AbstractNode : TreeType
    {
        public List<Field> Fields { get; set; } = new();
    }
}