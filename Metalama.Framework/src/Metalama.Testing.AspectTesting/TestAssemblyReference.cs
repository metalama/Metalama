// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Reflection;

namespace Metalama.Testing.AspectTesting
{
    internal static class ExtensionAssemblyReferenceExtensions
    {
        internal static PortableExecutableReference? ToMetadataReference( this TargetedAssemblyReference reference )
        {
            if ( reference.Path != null )
            {
                return MetadataReference.CreateFromFile( reference.Path! );
            }
            else
            {
                var assembly = AppDomainUtility
                                   .GetLoadedAssemblies( x => string.Equals( x.GetName().Name, reference.Name, StringComparison.OrdinalIgnoreCase ) )
                                   .FirstOrDefault()
                               ??
                               Assembly.Load( reference.Name );

                return MetadataReference.CreateFromFile( assembly.Location );
            }
        }
    }
}