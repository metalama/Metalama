// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32222;

public class MyAspect : OverrideMethodAspect
{
    private string _tag;

    public MyAspect( string tag )
    {
        this._tag = tag;
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( this._tag );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [MyAspect( "Direct" )]
    private void M() { }

    private class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
        {
            amender.SelectMany( t => t.Methods ).AddAspect( _ => new MyAspect( "Fabric" ) );
        }
    }
}