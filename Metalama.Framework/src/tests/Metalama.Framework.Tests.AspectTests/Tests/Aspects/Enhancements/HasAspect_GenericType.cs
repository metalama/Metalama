// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Enhancements.HasAspect_GenericType;

internal class MyAspect : TypeAspect
{
    [Introduce]
    private void Introduced()
    {
        Console.WriteLine( "Introduced" );
    }
}

internal class CheckerAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        // Find the field of type Foo<int>, which is a closed generic instance.
        var fooIntField = builder.Target.Fields.Single( f => f.Name == "_fooInt" );
        var fooIntType = (INamedType) fooIntField.Type;

        // This should return true because MyAspect is applied to Foo<T> (the open generic type).
        // Bug #753: This currently returns false for the closed generic instance.
        if ( !fooIntType.Enhancements().HasAspect<MyAspect>() )
        {
            throw new InvalidOperationException(
                $"HasAspect<MyAspect>() returned false for closed generic type '{fooIntType}', but the aspect is applied to the open generic type definition." );
        }

        // Also verify the non-generic overload.
        if ( !fooIntType.Enhancements().HasAspect( typeof(MyAspect) ) )
        {
            throw new InvalidOperationException(
                $"HasAspect(typeof(MyAspect)) returned false for closed generic type '{fooIntType}', but the aspect is applied to the open generic type definition." );
        }
    }
}

[MyAspect]
internal class Foo<T>
{
    public T? Value { get; set; }
}

// <target>
[CheckerAspect]
internal partial class TargetCode
{
    private Foo<int> _fooInt = new();
}
