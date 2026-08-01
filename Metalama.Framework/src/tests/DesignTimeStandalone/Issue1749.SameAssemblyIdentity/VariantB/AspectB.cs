// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Reflection;

// The same version as VariantA. Together with the same assembly name, this gives the two projects an equal
// AssemblyIdentity, which is the key that collides.
[assembly: AssemblyVersion( "1.0.0.0" )]

namespace SharedAspects;

/// <summary>
/// An inheritable aspect, so that this project contributes a transitive aspects manifest.
/// </summary>
/// <remarks>
/// Declared under the same name and namespace as the one in <c>VariantA</c>, which is legal because the consumer never
/// names it. See the README.
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
/// A target of <see cref="InheritedAspect"/>, named differently from the one in <c>VariantA</c>.
/// </summary>
[InheritedAspect]
public partial class ExportedBaseB { }
