// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using FluentAssertions;
using Metalama.Patterns.Observability.UnitTests.Assets.ChildPropertyAssets;
using Xunit;

namespace Metalama.Patterns.Observability.UnitTests;

public sealed class ChildPropertyTests : InpcTestsBase
{
    [Fact]
    public void FourDeep()
    {
        var a = new A();

        var sa = this.SubscribeTo( a );
        var sA2 = this.SubscribeTo( a.A2 );
        var sB2 = this.SubscribeTo( a.A2.B2 );
        var sC2 = this.SubscribeTo( a.A2.B2.C2 );

        // Reminder:
        // public int A3 => this.A2.B2.C2.D1;

        // 1. Change leaf value D1

        this.EventsFrom( () => a.A2.B2.C2.D1 = 1 )
            .Should()
            .Equal( (sa, "A3"), (sC2, "D1") );

        // 2. Change leaf parent ref, but leaf value is the same. This is notified as a change to A3 because
        // there is no false positive detection (we don't store a copy of the value of D1)

        var newD = new D() { D1 = a.A2.B2.C2.D1 };

        var sNewD = this.SubscribeTo( newD );

        this.EventsFrom( () => a.A2.B2.C2 = newD )
            .Should()
            .Equal( (sa, "A3"), (sB2, "C2") );

        sC2.Dispose();

        // ReSharper disable once RedundantAssignment
        sC2 = sNewD;

        // 3. Change leaf parent-parent ref, but parent is the same object. This is not notified as a change
        // to A3 because we have to store the last value of the parent object anyhow, and we detect that it is the
        // same ref.

        var newC = new C() { C2 = a.A2.B2.C2 };

        var sNewC = this.SubscribeTo( newC );

        this.EventsFrom( () => a.A2.B2 = newC )
            .Should()
            .Equal( (sA2, "B2") );

        sB2.Dispose();
        sB2 = sNewC;

        // 4. Change leaf parent-parent ref, parent is a different object, but leaf value D1 is the same.
        // This is notified as a change to A3 because we have to store the last value of the parent object anyhow,
        // and we detect that it is a different ref. But we don't have false positive detection so we
        // can't tell that leaf value D1 is actually the same value.

        var newC_2 = new C() { C2 = new D() { D1 = a.A2.B2.C2.D1 } };

        var sNewC_2 = this.SubscribeTo( newC_2 );

        this.EventsFrom( () => a.A2.B2 = newC_2 )
            .Should()
            .Equal( (sa, "A3"), (sA2, "B2") );

        sB2.Dispose();
        sB2 = sNewC_2;

        // 5. Change leaf parent-parent ref, parent is a different object, and leaf value D1 is a different value.
        // This is notified as a change to A3 because we have to store the last value of the parent object anyhow, and
        // we detect that it is a different ref.

        var newC_3 = new C() { C2 = new D() };

        var sNewC_3 = this.SubscribeTo( newC_3 );

        this.EventsFrom( () => a.A2.B2 = newC_3 )
            .Should()
            .Equal( (sa, "A3"), (sA2, "B2") );

        sB2.Dispose();

        // ReSharper disable once RedundantAssignment
        sB2 = sNewC_3;

        // 6. Change leaf parent-parent-parent ref. As per above comments, because we don't have false
        // positive detection, it does not matter if leaf value D1 is changed or not, a change is
        // notified. However, if the D ref in C2 is the same ref, even if the ancestor C and B refs are
        // different, then we can tell it's not a change.

        // 6.1 Change leaf parent-parent-parent ref and parent-parent ref, but keep same parent ref.
        // No change of A3 is notified, but A2 change is notified.

        var newB = new B() { B2 = new C() { C2 = a.A2.B2.C2 } };

        var sNewB = this.SubscribeTo( newB );

        this.EventsFrom( () => a.A2 = newB )
            .Should()
            .Equal( (sa, "A2") );

        sA2.Dispose();

        // ReSharper disable once RedundantAssignment
        sA2 = sNewB;

        // 6.2 Change leaf parent-parent-parent ref, parent-parent ref, and parent ref.
        // Change of both A3 and A2 is notified.

        var newB_2 = new B() { B2 = new C() { C2 = new D() } };

        // ReSharper disable once UnusedVariable
        var sNewB_2 = this.SubscribeTo( newB_2 );

        this.EventsFrom( () => a.A2 = newB_2 )
            .Should()
            .Equal( (sa, "A3"), (sa, "A2") );
    }

    [Fact]
    public void MultipleChildRefsAtDifferentLevels()
    {
        // NB: Remember, there is no false positive detection for leaf values.

        var e = new E();

        this.SubscribeTo( e );

        this.EventsFrom( () => e.E2.B2.C2.D1 = 1 )
            .Should()
            .Equal( "LR" );

        this.EventsFrom( () => e.E2.B2.C2 = new D() )
            .Should()
            .Equal( "LR" );

        this.EventsFrom( () => e.E2.B2 = new C() )
            .Should()
            .Equal( "LR", "LP1R" );

        this.EventsFrom( () => e.E2 = new B() )
            .Should()
            .Equal( "LR", "LP1R", "LP2R", "E2" );
    }

    [Fact]
    public void ChildRefFirstMonitoredInDerivedClass()
    {
        var g = new G();

        // ReSharper disable once UnusedVariable
        var sf = this.SubscribeTo( g );

        this.EventsFrom( () => g.F1.B2.C2.D1 = 1 )
            .Should()
            .Equal( "G1" );

        // New D, but with existing D1 value. A change is reported because we don't
        // have false +ve detection so don't store a copy of leaf value D1 and can't
        // tell if it has really changed.

        var newD = new D() { D1 = g.F1.B2.C2.D1 };

        this.EventsFrom( () => g.F1.B2.C2 = newD )
            .Should()
            .Equal( "G1" );

        // New C, but with existing D:

        var newC = new C() { C2 = g.F1.B2.C2 };

        this.EventsFrom( () => g.F1.B2 = newC )
            .Should()
            .Equal( "F2" );

        // New C, with new D:

        var newC_2 = new C();

        this.EventsFrom( () => g.F1.B2 = newC_2 )
            .Should()
            .Equal( "F2", "G1" );

        // New B, but with existing C. Because we don't have false +ve detection, class F does
        // not store the current value of F1.B2, so any change in F1 is assumed to be a change
        // to B2.

        var newB = new B() { B2 = g.F1.B2 };

        this.EventsFrom( () => g.F1 = newB )
            .Should()
            .Equal( "F2", "F1" );

        // New B, with new C:

        var newB_2 = new B();

        this.EventsFrom( () => g.F1 = newB_2 )
            .Should()
            .Equal( "F2", "G1", "F1" );
    }
}