// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.Multicast;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, use the <see cref="InheritableAttribute"/> custom attribute to enable aspect inheritance. By default, Metalama implements the
    /// <see cref="Strict"/> inheritance mode. If you need multicasting, see <see cref="MulticastAspect"/> or <see cref="MulticastImplementation"/>.
    /// </summary>
    public enum MulticastInheritance
    {
        /// <summary>
        /// This is still the default option.
        /// </summary>
        None,

        /// <summary>
        /// In Metalama, use the <see cref="InheritableAttribute"/> custom attribute.
        /// </summary>
        Strict,

        /// <summary>
        /// Multicast inheritance is not supported in Metalama, but it can be emulated by having the aspect implement
        /// <see cref="IAspect{T}"/> for <see cref="INamedType"/> and add the aspect to members.
        /// </summary>
        Multicast
    }
}