// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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