// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#nullable disable

using System;
using System.Xml.Serialization;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter.Model
{
    public sealed class Kind
    {
        [XmlAttribute]
        public string Name { get; set; }

        public override bool Equals( object obj )
            => obj is Kind kind &&
               this.Name == kind.Name;

        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => this.Name.GetHashCode( StringComparison.Ordinal );

        public override string ToString() => this.Name;
    }
}