// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#nullable disable

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    public sealed class Node : TreeType
    {
        [XmlAttribute]
        public string Root { get; set; }

        [XmlAttribute]
        public string Errors { get; set; }

        [XmlElement( ElementName = "Kind", Type = typeof(Kind) )]
        public List<Kind> Kinds { get; set; } = new();

        internal List<Field> Fields { get; } = new();
    }
}