// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Consumer;

/// <summary>
/// The consumer of the two <c>Aspects</c> assemblies.
/// </summary>
/// <remarks>
/// It names no type of either one. The collision happens while Metalama collects one transitive aspects manifest per
/// referenced assembly identity, which it does for every reference whether or not the consumer uses it, so the consumer
/// only has to have a pipeline of its own.
/// </remarks>
internal static class Program
{
    private static void Main() => System.Console.WriteLine( "ok" );
}

/// <summary>
/// An aspect of the consumer itself, so that the consumer runs a pipeline.
/// </summary>
public class OwnAspect : TypeAspect
{
    /// <summary>
    /// An introduced member, so that the aspect has an effect.
    /// </summary>
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetOwnMessage() => "own";
}

/// <summary>
/// The target of <see cref="OwnAspect"/>.
/// </summary>
[OwnAspect]
public partial class Target { }
