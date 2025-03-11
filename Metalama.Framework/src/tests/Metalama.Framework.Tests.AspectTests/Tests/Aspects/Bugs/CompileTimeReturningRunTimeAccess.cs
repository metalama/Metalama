// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeReturningRunTimeAccess;

internal class Aspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        base.BuildAspect( builder );

        builder.OverrideAccessors( nameof(OverrideGetter) );
    }

    [Template]
    private dynamic? OverrideGetter()
    {
        var field = ( meta.Target.Property.ToFieldOrPropertyInfo().GetCustomAttributes( true ).SingleOrDefault( x => x is IEntityField ) as IEntityField )
                    ?? throw new Exception( "Unable to retrieve field info." );

        Console.WriteLine( field );

        return meta.Proceed();
    }
}

internal interface IEntityField { }

internal class EntityFieldAttribute : Attribute, IEntityField { }

internal class Target
{
    // <target>
    [Aspect]
    [EntityField]
    public string? Name { get; set; }
}