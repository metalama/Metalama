// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Reflection;

[assembly: AssemblyVersion( "1.0.0.0" )]

namespace SharedAspects;

/// <summary>
/// An inheritable aspect, so that this project contributes a transitive aspects manifest.
/// </summary>
/// <remarks>
/// The manifest is what collides: <c>TransitivePipelineContributorSource</c> collects one manifest per referenced
/// assembly identity, and this project shares its identity with <c>VariantB</c>.
/// </remarks>
[Inheritable]
public class InheritedAspect : TypeAspect
{
    /// <summary>
    /// An introduced member, so that the aspect has an effect.
    /// </summary>
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetInheritedMessage() => "inherited";
}

/// <summary>
/// A target of <see cref="InheritedAspect"/>, named differently from the one in <c>VariantB</c> so that the two
/// assemblies are not interchangeable.
/// </summary>
[InheritedAspect]
public partial class ExportedBaseA { }
