// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentOutOfRangeException"/> if the target is assigned a value
/// smaller than zero. The behavior when the target is assigned to zero depends
/// on the <see cref="ContractOptions.DefaultInequalityStrictness"/> option. If this option
/// is not specified, a warning is reported.
/// </summary>
/// <remarks>
///     <para>Null values are accepted and do not throw an exception.
/// </para>
/// </remarks>
/// <seealso cref="NonNegativeAttribute"/>
/// <seealso cref="StrictlyPositiveAttribute"/>
/// <seealso href="@contract-types"/>
[PublicAPI]
[RunTimeOrCompileTime]
public class PositiveAttribute : GreaterThanAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PositiveAttribute"/> class.
    /// </summary>
    public PositiveAttribute() : base( 0 ) { }

    private protected override InequalityAmbiguity Ambiguity
        => new(
            InequalityStrictness.NonStrict,
            nameof(NonNegativeAttribute),
            nameof(StrictlyPositiveAttribute) );
}