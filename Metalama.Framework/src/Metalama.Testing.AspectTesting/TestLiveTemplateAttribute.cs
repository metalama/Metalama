// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// A custom that must be used in the <see cref="TestScenario.LiveTemplate"/> and <see cref="TestScenario.LiveTemplatePreview"/>
/// to mark the declaration to which the aspect must be applied. The presence of this attribute simulates the use of the refactoring
/// context menu.
/// </summary>
[AttributeUsage( AttributeTargets.All )]
[PublicAPI]
public class TestLiveTemplateAttribute : Attribute
{
    public TestLiveTemplateAttribute( Type aspectType )
    {
        this.AspectType = aspectType;
    }

    public Type AspectType { get; }
}