// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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