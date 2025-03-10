// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.ComponentModel;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue31071;

public abstract class RootXPObject
{
    public virtual void AfterConstruction() { }
}

public abstract class BaseXPObject : RootXPObject
{
    public override void AfterConstruction()
    {
        base.AfterConstruction();
    }
}

// <target>
[XpoDefaultValueAutoImplementation]
public sealed class MyXpObject : BaseXPObject { }

public sealed class XpoDefaultValueAutoImplementationAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod(
            nameof(AfterConstruction),
            whenExists: OverrideStrategy.Override );
    }

    [Template]
    public void AfterConstruction()
    {
        Console.WriteLine( "Overridden!" );

        meta.Proceed();
    }

    private TypedConstant GetDefaultValue( IFieldOrProperty field )
    {
        var defaultValueAttribute = field.Attributes.FirstOrDefault( a => a.Type.IsConvertibleTo( typeof(DefaultValueAttribute) ) );

        if (defaultValueAttribute is null)
        {
            throw new InvalidOperationException( "Could not get DefaultValueAttribute!?" );
        }

        var value = defaultValueAttribute.ConstructorArguments.Length == 1
            ? defaultValueAttribute.ConstructorArguments[0]
            : defaultValueAttribute.ConstructorArguments[1];

        return value;
    }
}