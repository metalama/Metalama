// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Consumer;

/// <summary>
/// An aspect of the consumer itself, whose only role is to make the consumer serialize a compile-time type name.
/// </summary>
/// <remarks>
/// The closure dictionary is built for the whole closure on the first lookup of any key, so the consumer does not have
/// to serialize a type of <c>Contract</c> to hit the duplicate. Serializing its own aspect is enough, and it keeps the
/// consumer from ever naming the ambiguous type, which would be <c>CS0433</c>.
/// </remarks>
[Inheritable]
public class ConsumerAspect : TypeAspect
{
    /// <summary>
    /// An introduced member, so that the aspect has an effect.
    /// </summary>
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetMessage() => "consumer";
}

/// <summary>
/// The target of <see cref="ConsumerAspect"/>. Being inheritable, the aspect is written to the transitive manifest,
/// which is what serializes its compile-time type name.
/// </summary>
[ConsumerAspect]
public partial class BaseClass { }
