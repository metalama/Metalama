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

        /// <summary>
        /// Gets or sets the address of the issue that tracks the experimental feature this node belongs to.
        /// </summary>
        /// <remarks>
        /// Roslyn annotates the corresponding API with <c>ExperimentalAttribute</c>, so referring to it from generated
        /// code raises an <c>RSEXPERIMENTAL</c> error. Experimental features are not supported, so <see cref="TreeReader"/>
        /// removes every node that carries this attribute.
        /// </remarks>
        [XmlAttribute]
        public string ExperimentalUrl { get; set; }

        /// <summary>
        /// Gets a value indicating whether the node belongs to an experimental feature.
        /// </summary>
        public bool IsExperimental => !string.IsNullOrEmpty( this.ExperimentalUrl );

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