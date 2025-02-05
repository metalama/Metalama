// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// Represents a metadata reference. This class is JSON-serializable.
    /// </summary>
    [JsonObject]
    public sealed class TestAssemblyReference
    {
        public string? Path { get; set; }

        public string? Name { get; set; }

        internal PortableExecutableReference? ToMetadataReference()
        {
            if ( this.Path != null )
            {
                return MetadataReference.CreateFromFile( this.Path! );
            }
            else if ( this.Name != null )
            {
                var assembly = AppDomainUtility.GetLoadedAssemblies( x => string.Equals( x.GetName().Name, this.Name, StringComparison.OrdinalIgnoreCase ) )
                                   .FirstOrDefault()
                               ??
                               Assembly.Load( this.Name );

                return MetadataReference.CreateFromFile( assembly.Location );
            }

            return null;
        }

        public override string ToString() => this.Path ?? "<null>";
    }
}