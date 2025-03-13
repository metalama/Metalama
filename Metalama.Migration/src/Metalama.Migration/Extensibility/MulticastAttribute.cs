// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.Multicast;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// Okay, this one is problematic. There is no declarative (attribute-based) multicasting in Metalama. Instead, fabrics should be used.
    /// However, we have ported the main features of <see cref="MulticastAttribute"/> to Metalama. See <see cref="MulticastAspect"/> or <see cref="MulticastImplementation"/>.
    /// </summary>
    /// <seealso href="@fabrics-aspects"/>
    public abstract class MulticastAttribute : Attribute
    {
        public MulticastTargets AttributeTargetElements { get; set; }

        [Obsolete( "Adding aspects to external assemblies is not supported in Metalama.", true )]
        public string AttributeTargetAssemblies { get; set; }

        public string AttributeTargetTypes { get; set; }

        public MulticastAttributes AttributeTargetTypeAttributes { get; set; }

        [Obsolete( "Adding aspects to external assemblies is not supported in Metalama.", true )]
        public MulticastAttributes AttributeTargetExternalTypeAttributes { get; set; }

        public string AttributeTargetMembers { get; set; }

        public MulticastAttributes AttributeTargetMemberAttributes { get; set; }

        [Obsolete( "Adding aspects to external assemblies is not supported in Metalama.", true )]
        public MulticastAttributes AttributeTargetExternalMemberAttributes { get; set; }

        public string AttributeTargetParameters { get; set; }

        public MulticastAttributes AttributeTargetParameterAttributes { get; set; }

        public bool AttributeExclude { get; set; }

        public int AttributePriority { get; set; }

        /// <summary>
        /// Metalama port of <see cref="MulticastAttribute"/> does not support adding several instances of an aspect on the same declaration,
        /// so this property is always considered <c>true</c> regardless of its actual value.
        /// </summary>
        public bool AttributeReplace { get; set; }

        public MulticastInheritance AttributeInheritance { get; set; }

        [Obsolete( "Do not use this property in customer code.", true )]

        public long AttributeId { get; set; }
    }
}