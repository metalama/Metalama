// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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