// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Eligibility;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, the valid targets are determined by the declaration types for which the <see cref="IAspect{T}"/> interface is implemented.
    /// Additionally, you can implement the <see cref="IEligible{T}.BuildEligibility"/> method. However, the main features of <see cref="MulticastAttribute"/>
    /// have been ported to Metalama. The equivalent to this attribute is <see cref="Metalama.Extensions.Multicast.MulticastTargets"/>.
    /// </summary>
    [Flags]
    public enum MulticastTargets
    {
        Default = 0,

        Class = 1,

        Struct = 2,

        Enum = 4,

        Delegate = 8,

        Interface = 16,

        AnyType = Class | Struct | Enum | Delegate | Interface,

        Field = 32,

        Method = 64,

        InstanceConstructor = 128,

        StaticConstructor = 256,

        Property = 512,

        Event = 1024,

        AnyMember = Field | Method | InstanceConstructor | StaticConstructor | Property | Event,

        Assembly = 2048,

        Parameter = 4096,

        ReturnValue = 8192,

        All = Assembly | AnyMember | AnyType | Parameter | ReturnValue
    }
}