// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#nullable disable

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    [PublicAPI]
    public sealed class Field : TreeTypeChild
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public string Optional { get; set; }

        [XmlAttribute]
        public string Override { get; set; }

        [XmlAttribute]
        public string New { get; set; }

        [XmlAttribute]
        public int MinCount { get; set; }

        [XmlAttribute]
        public bool AllowTrailingSeparator { get; set; }

        [XmlElement( ElementName = "Kind", Type = typeof(Kind) )]
        public List<Kind> Kinds { get; set; } = new();

        [XmlElement]
        public Comment PropertyComment { get; set; }

        public bool IsToken => this.Type == "SyntaxToken";

        public bool IsOptional => this.Optional == "true";

        [XmlIgnore]
        internal RoslynVersion MinimalRoslynVersion { get; set; }

        [XmlIgnore]
        internal RoslynVersion MaximalRoslynVersion { get; set; }

        [XmlIgnore]
        internal Dictionary<Kind, RoslynVersion> KindsMinimalRoslynVersions { get; set; }
    }
}