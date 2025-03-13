// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentOutOfRangeException"/> if the target is assigned a value
/// greater than or equal to zero.
/// </summary>
/// <remarks>
///     <para>Null values are accepted and do not throw an exception.
/// </para>
/// </remarks>
/// <seealso href="@contract-types"/>
[PublicAPI]
[RunTimeOrCompileTime]
public class StrictlyNegativeAttribute : StrictlyLessThanAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyNegativeAttribute"/> class.
    /// </summary>
    public StrictlyNegativeAttribute() : base( 0 ) { }
}