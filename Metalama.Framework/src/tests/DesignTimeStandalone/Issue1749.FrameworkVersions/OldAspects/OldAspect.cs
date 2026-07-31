// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace OldAspects;

/// <summary>
/// An inheritable aspect compiled against the old, publicly released version of Metalama.Framework.
/// </summary>
/// <remarks>
/// The aspect is inheritable so that it reaches the consumer through the transitive manifest, which is serialized
/// by the compile-time serializer and therefore goes through <c>CompileTimeSerializationBinder.BindToName</c>.
/// </remarks>
[Inheritable]
public class OldAspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetOldMessage() => "old";
}

/// <summary>
/// The base class that carries <see cref="OldAspect"/> to the consumer.
/// </summary>
[OldAspect]
public class OldBaseClass { }
