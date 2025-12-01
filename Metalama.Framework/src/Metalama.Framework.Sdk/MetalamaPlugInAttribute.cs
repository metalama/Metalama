// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.AspectWeavers;
using System;

namespace Metalama.Framework.Engine;

/// <summary>
/// Marks a class as a Metalama plug-in that extends the compiler pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Classes marked with this attribute are discovered and instantiated by Metalama during compilation.
/// The class must have a public parameterless constructor.
/// </para>
/// <para>
/// This attribute is used for:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IAspectWeaver"/> implementations: Low-level aspect weavers that perform Roslyn-based transformations.</description></item>
/// <item><description>Custom metric providers: Classes deriving from <c>MetricProvider&lt;T&gt;</c> or <c>SyntaxMetricProvider&lt;T&gt;</c>.</description></item>
/// </list>
/// <para>
/// Plug-in classes must be in an assembly that references <c>Metalama.Framework.Sdk</c> with <c>PrivateAssets="all"</c>.
/// </para>
/// </remarks>
/// <seealso cref="IAspectWeaver"/>
/// <seealso href="@aspect-weavers"/>
/// <seealso href="@custom-metrics"/>
[AttributeUsage( AttributeTargets.Class )]
[CompileTime]
[PublicAPI]
public sealed class MetalamaPlugInAttribute : Attribute;