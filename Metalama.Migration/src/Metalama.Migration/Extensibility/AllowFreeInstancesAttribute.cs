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
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Assembly )]
    [PublicAPI]
    public sealed class AllowFreeInstancesAttribute : Attribute
    {
        public AllowFreeInstancesAttribute( string featureName )
        {
            throw new NotImplementedException();
        }

        public AllowFreeInstancesAttribute( string featureName, int count )
        {
            throw new NotImplementedException();
        }

        public string FeatureName { get; }

        public int? FreeInstanceCount { get; }

        public bool ApplyToType { get; set; }
    }
}