// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#nullable disable

using System.Xml;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    public sealed class Comment
    {
        [XmlAnyElement]
        public XmlElement[] Body { get; set; }
    }
}