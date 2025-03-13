// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#nullable disable

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    [XmlRoot]
    public class Tree
    {
        [XmlAttribute]
        public string Root { get; set; }

        [XmlElement( ElementName = "Node", Type = typeof(Node) )]
        [XmlElement( ElementName = "AbstractNode", Type = typeof(AbstractNode) )]
        [XmlElement( ElementName = "PredefinedNode", Type = typeof(PredefinedNode) )]
        public List<TreeType> Types { get; set; } = new();
    }
}