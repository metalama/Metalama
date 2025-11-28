// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.AspectWeavers;
using System;

namespace Metalama.Framework.Engine;

/// <summary>
/// Indicates that a class is a Metalama plug-in. When applied to a class, Metalama will create an instance
/// of that class and expose it as a service through <see cref="IServiceProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is typically used to mark classes that implement <see cref="IAspectWeaver"/> or provide
/// custom services for Metalama. Classes marked with this attribute must have a public parameterless constructor.
/// </para>
/// </remarks>
/// <seealso cref="IAspectWeaver"/>
[AttributeUsage( AttributeTargets.Class )]
[CompileTime]
[PublicAPI]
public sealed class MetalamaPlugInAttribute : Attribute;