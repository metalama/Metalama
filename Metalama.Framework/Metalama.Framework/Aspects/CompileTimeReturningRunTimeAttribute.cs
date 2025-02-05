// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Attribute that means that the target method is compile-time but returns a run-time value.
/// </summary>
[AttributeUsage( AttributeTargets.Method )]
internal sealed class CompileTimeReturningRunTimeAttribute : ScopeAttribute;