// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace PostSharp.Aspects.Configuration
{
    /// <summary>
    /// There is no aspect configuration in Metalama.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class )]
    public class AspectConfigurationAttribute : Attribute
    {
        /// <summary>
        /// There is no numeric aspect priority in Metalama. Use <see cref="AspectOrderAttribute"/>. 
        /// </summary>
        /// <seealso href="@ordering"/>
        public int AspectPriority { get; set; }

        /// <summary>
        /// In Metalama, aspects are also serializable, but for different reasons. Serialization is not configurable.
        /// </summary>
        public Type SerializerType { get; set; }

        protected virtual AspectConfiguration CreateAspectConfiguration()
        {
            return new AspectConfiguration();
        }

        public AspectConfiguration GetAspectConfiguration()
        {
            throw new NotImplementedException();
        }

        protected virtual void SetAspectConfiguration( AspectConfiguration aspectConfiguration )
        {
            throw new NotImplementedException();
        }
    }
}