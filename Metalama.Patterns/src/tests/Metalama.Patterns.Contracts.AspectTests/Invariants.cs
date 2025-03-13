// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Contracts.AspectTests.Invariants;

public class BaseClass
{
    [Invariant]
    private void TheInvariant()
    {
        if ( this.A + this.B != 0 )
        {
            throw new InvariantViolationException();
        }
    }

    public void SomePublicMethod() { }

    protected void SomeProtectedMethod() { }

    protected internal void SomeProtectedInternalMethod() { }

    internal void SomeInternalMethod() { }

    private void SomePrivateMethod() { }

    private void SomeReadOnlyMethod() { }

    public int A { get; set; }

    public int B { get; set; }
}

public class DerivedClass : BaseClass
{
    [Invariant]
    private void OtherInvariant()
    {
        if ( this.A < this.C )
        {
            throw new InvariantViolationException();
        }
    }

    [DoNotCheckInvariants]
    public void NoInvariant() { }

    public int C { get; set; }
}