// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// An attribute that must be used in the <see cref="TestScenario.LiveTemplate"/> and <see cref="TestScenario.LiveTemplatePreview"/>
/// to mark the declaration to which the aspect must be applied. The presence of this attribute simulates the use of the refactoring
/// context menu.
/// </summary>
[AttributeUsage( AttributeTargets.All )]
[PublicAPI]
public class TestLiveTemplateAttribute( Type aspectType ) : Attribute
{
    public Type AspectType { get; } = aspectType;
}