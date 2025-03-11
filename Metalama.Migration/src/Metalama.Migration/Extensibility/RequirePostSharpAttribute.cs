// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, use <see cref="RequireAspectWeaverAttribute"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly, AllowMultiple = true )]
    [PublicAPI]
    public sealed class RequirePostSharpAttribute : Attribute
    {
        public RequirePostSharpAttribute( string plugIn, string task )
        {
            throw new NotImplementedException();
        }

        public RequirePostSharpAttribute( string plugIn )
        {
            throw new NotImplementedException();
        }

        public RequirePostSharpAttribute( Type serviceType )
        {
            this.ServiceType = serviceType;
        }

        public string PlugIn { get; }

        public string Task { get; }

        public Type ServiceType { get; }

        public bool AssemblyReferenceOnly { get; set; }

        public bool AnyTypeReference { get; set; }
    }
}