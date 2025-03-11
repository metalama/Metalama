// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// When applied to a template method parameter, indicates that the introduced parameter should have the <see langword="this"/> modifier, and that the introduced method should be an extension method.
/// </summary>
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class ThisAttribute : Attribute;