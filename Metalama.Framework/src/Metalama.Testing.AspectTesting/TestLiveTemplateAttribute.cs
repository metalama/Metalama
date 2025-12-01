// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// An attribute used in <see cref="TestScenario.LiveTemplate"/> and <see cref="TestScenario.LiveTemplatePreview"/> tests
/// to mark the declaration to which an aspect should be applied as a live template.
/// </summary>
/// <remarks>
/// <para>
/// In live template test scenarios, this attribute simulates the user selecting a declaration in the IDE's refactoring
/// context menu and applying an aspect to it. The attribute specifies which aspect type should be applied.
/// </para>
/// <para>
/// To use this attribute, set the <c>// @TestScenario(LiveTemplate)</c> or <c>// @TestScenario(LiveTemplatePreview)</c>
/// comment in your test file, then apply this attribute to the target declaration.
/// </para>
/// </remarks>
/// <seealso cref="TestScenario.LiveTemplate"/>
/// <seealso cref="TestScenario.LiveTemplatePreview"/>
/// <seealso href="@aspect-testing"/>
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