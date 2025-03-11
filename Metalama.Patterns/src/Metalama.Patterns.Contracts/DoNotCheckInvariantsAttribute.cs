// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when applied to a method, means that invariants should not be checked when this method completes.
/// </summary>
/// <remarks>
/// This custom attribute does not caused methods <i>called</i> by the target method to skip invariant checks.
/// For this, enable the <see cref="ContractOptions.IsInvariantSuspensionSupported"/> contract option and call the generated <c>SuspendInvariant</c>
/// method or the <see cref="SuspendInvariantsAttribute"/> aspect.
/// </remarks>
/// <seealso href="@invariants"/>
[AttributeUsage( AttributeTargets.Method )]
[PublicAPI]
public sealed class DoNotCheckInvariantsAttribute : Attribute;