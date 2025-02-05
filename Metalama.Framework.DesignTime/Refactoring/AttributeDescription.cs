// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Refactoring
{
    public sealed class AttributeDescription
    {
        public AttributeDescription(
            string name,
            ImmutableList<string>? constructorArguments = null,
            ImmutableList<(string Name, string Value)>? namedArguments = null,
            ImmutableList<string>? imports = null )
        {
            this.Name = name;
            this.Properties = namedArguments ?? ImmutableList<(string, string)>.Empty;
            this.Arguments = constructorArguments ?? ImmutableList<string>.Empty;
            this.Imports = imports ?? ImmutableList<string>.Empty;
        }

        internal string Name { get; }

        // We don't use dictionary here to preserve the order
        internal ImmutableList<(string Name, string Value)> Properties { get; }

        internal ImmutableList<string> Arguments { get; }

        internal ImmutableList<string> Imports { get; }
    }
}