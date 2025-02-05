// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Attribute that means that the target declaration (and all children declarations) can only be called from run-time
/// code and, therefore, not from compile-time code. Code is run-time by default, so this attribute only makes sense on classes or interface that
/// are run-time-only but derive a run-time-or-compile-time type. See <see cref="RunTimeOrCompileTimeAttribute"/>.
/// </summary>
[PublicAPI]
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Interface
    | AttributeTargets.Assembly | AttributeTargets.ReturnValue | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field
    | AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.GenericParameter )]
public sealed class RunTimeAttribute : ScopeAttribute;