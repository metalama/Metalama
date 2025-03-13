// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Contracts;

/// <summary>
/// An exception that should be thrown by the target methods of the <see cref="InvariantAttribute"/> aspect.
/// </summary>
/// <seealso href="@invariants"/>
public class InvariantViolationException : ApplicationException
{
    public InvariantViolationException() { }

    public InvariantViolationException( string message ) : base( message ) { }

    public InvariantViolationException( string message, Exception innerException ) : base( message, innerException ) { }
}