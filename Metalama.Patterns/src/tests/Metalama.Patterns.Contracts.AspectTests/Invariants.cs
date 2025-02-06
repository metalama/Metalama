// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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