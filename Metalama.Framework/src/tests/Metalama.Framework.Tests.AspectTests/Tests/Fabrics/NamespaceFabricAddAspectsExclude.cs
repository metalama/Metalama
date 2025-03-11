// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.NamespaceFabricAddAspectsExclude;

internal class Fabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender
            .SelectMany( c => c.DescendantsAndSelf() )
            .SelectMany( t => t.Types )
            .SelectMany( t => t.Methods )
            .Where( m => m.ReturnType.IsConvertibleTo( typeof(string) ) )
            .AddAspect<Aspect>();
    }
}

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "overridden" );

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private int Method1( int a )
    {
        return a;
    }

    private string Method2( string s )
    {
        return s;
    }
}

[ExcludeAspect( typeof(Aspect) )]
internal class ExcludedCode
{
    private int Method1( int a )
    {
        return a;
    }

    private string Method2( string s )
    {
        return s;
    }
}