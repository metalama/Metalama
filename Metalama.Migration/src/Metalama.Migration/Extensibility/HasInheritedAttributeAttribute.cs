// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface |
        AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
    [PublicAPI]
    public sealed class HasInheritedAttributeAttribute : Attribute
    {
        public HasInheritedAttributeAttribute() { }

        [Obsolete( "Do not use this custom attribute in user code.", false )]
        public HasInheritedAttributeAttribute( long[] ids ) { }
    }
}