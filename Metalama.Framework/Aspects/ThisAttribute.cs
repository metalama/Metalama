// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// When applied to a template method parameter, indicates that the introduced parameter should have the <see langword="this"/> modifier, and that the introduced method should be an extension method.
/// </summary>
[AttributeUsage( AttributeTargets.Parameter )]
public class ThisAttribute : Attribute;