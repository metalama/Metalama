// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model;

public sealed class Choice : TreeTypeChild
{
    // Note: 'Choice's should not be children of a 'Choice'.  It's not necessary, and the child
    // choice can just be inlined into the parent.
    [XmlElement( ElementName = "Field", Type = typeof(Field) )]
    [XmlElement( ElementName = "Sequence", Type = typeof(Sequence) )]
    public List<TreeTypeChild> Children { get; set; } = new();

    [XmlAttribute]
    public bool Optional { get; set; }
}