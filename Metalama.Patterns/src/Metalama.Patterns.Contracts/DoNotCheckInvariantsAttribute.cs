// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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