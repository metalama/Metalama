// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// There is no equivalent in Metalama. This logic is currently hard-coded.
    /// </summary>
    [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
    [PublicAPI]
    public sealed class ImplementationBoundAttributeAttribute : Attribute
    {
        public ImplementationBoundAttributeAttribute( Type attributeType )
        {
            throw new NotImplementedException();
        }

        public Type AttributeType { get; }
    }
}