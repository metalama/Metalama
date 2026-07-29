// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Middle2;

/// <summary>
/// An inheritable aspect that consumes a compile-time type of version 1.1 of the <c>Contract</c> assembly.
/// </summary>
/// <remarks>
/// The aspect is inheritable so that it reaches the consumer through the transitive manifest, which is serialized
/// with the compile-time serializer and therefore goes through <c>CompileTimeSerializationBinder.BindToName</c>.
/// </remarks>
[Inheritable]
public class Aspect2 : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetMessage2() => Contract.Helper.Message;
}

/// <summary>
/// The base class that carries <see cref="Aspect2"/> to the consumer.
/// </summary>
[Aspect2]
public class BaseClass2 { }
