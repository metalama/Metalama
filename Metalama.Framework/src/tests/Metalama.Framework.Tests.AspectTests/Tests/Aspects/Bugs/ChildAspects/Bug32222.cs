// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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