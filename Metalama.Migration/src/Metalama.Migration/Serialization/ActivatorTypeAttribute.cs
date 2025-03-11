// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Serialization
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [Obsolete( "", true )]
    [AttributeUsage( AttributeTargets.Assembly )]
    public sealed class ActivatorTypeAttribute : Attribute
    {
        public ActivatorTypeAttribute( Type activatorType )
        {
            this.ActivatorType = activatorType;
        }

        public Type ActivatorType { get; }
    }
}