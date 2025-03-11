// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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