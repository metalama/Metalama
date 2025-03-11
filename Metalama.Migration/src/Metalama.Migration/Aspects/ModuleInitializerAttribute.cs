// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// Not supported in Metalama, but it is now supported by C# itself.
    /// </summary>
    [AttributeUsage( AttributeTargets.Method )]
    [RequirePostSharp( null, "ModuleInitializer" )]
    [PublicAPI]
    public sealed class ModuleInitializerAttribute : Attribute
    {
        public ModuleInitializerAttribute( int order )
        {
            throw new NotImplementedException();
        }

        public int Order { get; }
    }
}