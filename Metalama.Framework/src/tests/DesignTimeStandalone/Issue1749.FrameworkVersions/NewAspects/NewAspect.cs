// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace NewAspects;

/// <summary>
/// An inheritable aspect compiled against the version of Metalama.Framework built by this repository.
/// </summary>
[Inheritable]
public class NewAspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetNewMessage() => "new";
}

/// <summary>
/// The base class that carries <see cref="NewAspect"/> to the consumer.
/// </summary>
[NewAspect]
public class NewBaseClass { }
