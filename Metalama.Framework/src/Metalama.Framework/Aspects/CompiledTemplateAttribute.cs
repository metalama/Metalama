// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Internal attribute added by Metalama during compilation to preserve the original characteristics of templates.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is internal to the Metalama infrastructure and should not be used in user code. When Metalama compiles
/// T# templates into standard C# code that generates run-time code using the Roslyn API, some characteristics of the
/// template methods (such as accessibility, async state, and iterator state) may be modified during the compilation
/// process. This attribute stores the original values so they can be referenced during template expansion.
/// </para>
/// <para>
/// As part of the T# compilation process, Metalama separates compile-time code from run-time code and embeds the
/// compile-time code (including compiled templates) as a managed resource in the run-time assembly. This attribute
/// ensures that template metadata remains accurate throughout this transformation.
/// </para>
/// </remarks>
/// <seealso cref="TemplateAttribute"/>
/// <seealso href="@template-overview"/>
/// <seealso href="@templates"/>
[CompileTime]
[AttributeUsage( AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property )]
public sealed class CompiledTemplateAttribute : Attribute
{
    public Accessibility Accessibility { get; set; }

    public bool IsIteratorMethod { get; set; }

    public bool IsAsync { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a property template uses the C# 14 <c>field</c> keyword,
    /// requiring Metalama to introduce a backing field when the template is applied.
    /// </summary>
    public bool IntroducesBackingField { get; set; }
}