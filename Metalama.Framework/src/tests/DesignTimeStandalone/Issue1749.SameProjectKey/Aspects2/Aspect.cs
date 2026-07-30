// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Aspects2;

/// <summary>
/// An inheritable aspect that the consumer receives through inheritance.
/// </summary>
/// <remarks>
/// This is the project the consumer references. Because it shares its <c>ProjectKey</c> with <c>Aspects1</c>, the
/// consumer's pipeline finds the pipeline of <c>Aspects1</c> under that key and runs it on <em>this</em> project's
/// compilation.
/// </remarks>
[Inheritable]
public class Aspect2 : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetMessage2() => "Aspects2";
}

/// <summary>
/// The base class that carries <see cref="Aspect2"/> to the consumer.
/// </summary>
[Aspect2]
public partial class BaseClass2 { }
