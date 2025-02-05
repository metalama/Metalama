// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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