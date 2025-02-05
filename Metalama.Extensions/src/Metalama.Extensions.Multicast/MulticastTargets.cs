// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Extensions.Multicast;

/// <summary>
///   Kinds of targets to which multicast aspects (<see cref = "IMulticastAttribute" />) can be applied.
/// </summary>
[Flags]
[RunTimeOrCompileTime]
[PublicAPI]
public enum MulticastTargets
{
    /// <summary>
    ///   Specifies that the set of target elements is inherited from
    ///   the parent custom attribute.
    /// </summary>
    Default = 0,

    /// <summary>
    ///   Class.
    /// </summary>
    Class = 1,

    /// <summary>
    ///   Structure.
    /// </summary>
    Struct = 2,

    /// <summary>
    ///   Enumeration.
    /// </summary>
    Enum = 4,

    /// <summary>
    ///   Delegate.
    /// </summary>
    Delegate = 8,

    /// <summary>
    ///   Interface.
    /// </summary>
    Interface = 16,

    /// <summary>
    ///   Any type (<see cref = "Class" />, <see cref = "Struct" />, <see cref = "Enum" />,
    ///   <see cref = "Delegate" /> or <see cref = "Interface" />).
    /// </summary>
    AnyType = Class | Struct | Enum | Delegate | Interface,

    /// <summary>
    ///   Field.
    /// </summary>
    Field = 32,

    /// <summary>
    ///   Method (but not constructor).
    /// </summary>
    Method = 64,

    /// <summary>
    ///   Instance constructor.
    /// </summary>
    InstanceConstructor = 128,

    /// <summary>
    ///   Static constructor.
    /// </summary>
    StaticConstructor = 256,

    /// <summary>
    ///   Property (but not methods inside the property).
    /// </summary>
    Property = 512,

    /// <summary>
    ///   Event (but not methods inside the event).
    /// </summary>
    Event = 1024,

    /// <summary>
    ///   Any member (<see cref = "Field" />, <see cref = "Method" />, <see cref = "InstanceConstructor" />,
    ///   <see cref = "StaticConstructor" />,
    ///   <see cref = "Property" />, <see cref = "Event" />).
    /// </summary>
    AnyMember = Field | Method | InstanceConstructor | StaticConstructor | Property | Event,

    /// <summary>
    ///   Assembly.
    /// </summary>
    Assembly = 2048,

    /// <summary>
    ///   Method or property parameter.
    /// </summary>
    Parameter = 4096,

    /// <summary>
    ///   Method or property return value.
    /// </summary>
    ReturnValue = 8192,

    /// <summary>
    ///   All element kinds.
    /// </summary>
    All = Assembly | AnyMember | AnyType | Parameter | ReturnValue
}