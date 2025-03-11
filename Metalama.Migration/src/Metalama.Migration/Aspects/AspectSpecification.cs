// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using PostSharp.Aspects.Configuration;
using PostSharp.Reflection;

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no equivalent to this class in Metalama.
    /// </summary>
    [PublicAPI]
    public class AspectSpecification
    {
        public AspectConfiguration AspectConfiguration { get; }

        public ObjectConstruction AspectConstruction { get; }

        public IAspect Aspect { get; }

        public string AspectAssemblyQualifiedTypeName { get; }

        public string AspectTypeName { get; }
    }
}