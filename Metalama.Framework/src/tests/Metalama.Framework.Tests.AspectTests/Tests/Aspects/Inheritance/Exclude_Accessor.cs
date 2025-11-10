// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.Exclude_Accessor;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() 
    {
        Console.WriteLine( "Overridden!" );

        return meta.Proceed();
    }
}

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectTypes()
            .SelectMany( t => t.FieldsAndProperties )
            .Where( p => !p.IsImplicitlyDeclared  )
            .SelectMany( p => p.Accessors )
            .AddAspect<Aspect>();
    }
}

// <target>
internal class Targets
{
    public int A { get; set; }

    [ExcludeAspect( typeof(Aspect) )]
    public int B { get; set; }

    public int C
    {
        get;
        [ExcludeAspect( typeof(Aspect) )]
        set;
    }
}