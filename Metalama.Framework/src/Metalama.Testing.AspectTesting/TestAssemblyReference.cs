// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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