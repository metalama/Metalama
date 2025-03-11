// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, all custom attributes except Metalama ones are copied from the template to the introduced declaration.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Method )]
    [PublicAPI]
    public sealed class CopyCustomAttributesAttribute : Advice
    {
        public CopyCustomAttributesAttribute( Type type )
        {
            throw new NotImplementedException();
        }

        public CopyCustomAttributesAttribute( params Type[] types )
        {
            throw new NotImplementedException();
        }

        public CustomAttributeOverrideAction OverrideAction { get; set; }

        public Type[] Types { get; }
    }
}