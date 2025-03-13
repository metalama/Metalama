// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#nullable disable

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    public class TreeType
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Base { get; set; }

        [XmlAttribute]
        public string SkipConvenienceFactories { get; set; }

        [XmlElement]
        public Comment TypeComment { get; set; }

        [XmlElement]
        public Comment FactoryComment { get; set; }

        [XmlElement( ElementName = "Field", Type = typeof(Field) )]
        [XmlElement( ElementName = "Choice", Type = typeof(Choice) )]
        [XmlElement( ElementName = "Sequence", Type = typeof(Sequence) )]
        public List<TreeTypeChild> Children { get; set; } = new();

        [XmlIgnore]
        internal RoslynVersion MinimalRoslynVersion { get; set; }
    }
}