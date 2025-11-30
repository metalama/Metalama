// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// When applied to a template method parameter, indicates that the introduced parameter should have the <see langword="this"/> modifier,
/// making the introduced method an extension method.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on the first parameter of a template method when you want the introduced method to be an extension method.
/// The parameter type determines the extended type.
/// </para>
/// <para>
/// The introduced method must be static.
/// </para>
/// </remarks>
/// <seealso cref="TemplateAttribute"/>
/// <seealso cref="IntroduceAttribute"/>
/// <seealso href="@introducing-members"/>
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class ThisAttribute : Attribute;