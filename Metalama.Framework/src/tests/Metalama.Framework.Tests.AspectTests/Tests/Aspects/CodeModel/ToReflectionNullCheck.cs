// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.ToReflectionNullCheck;

// Tests that ToMethodInfo(), ToFieldInfo(), ToPropertyInfo(), ToEventInfo(), ToConstructorInfo()
// generate null-checking code that throws helpful exceptions instead of returning null.

public sealed class TestAspect : TypeAspect
{
    [Introduce]
    public void Run()
    {
        // These should generate null-checked reflection lookups.
        var methodInfo = meta.RunTime( meta.Target.Type.Methods.OfName( "M" ).First().ToMethodInfo() );
        var fieldInfo = meta.RunTime( meta.Target.Type.Fields.OfName( "_field" ).First().ToFieldInfo() );
        var propertyInfo = meta.RunTime( meta.Target.Type.Properties.OfName( "P" ).First().ToPropertyInfo() );
        var eventInfo = meta.RunTime( meta.Target.Type.Events.OfName( "E" ).First().ToEventInfo() );
        var constructorInfo = meta.RunTime( meta.Target.Type.Constructors.First().ToConstructorInfo() );

        Console.WriteLine( methodInfo.Name );
        Console.WriteLine( fieldInfo.Name );
        Console.WriteLine( propertyInfo.Name );
        Console.WriteLine( eventInfo.Name );
        Console.WriteLine( constructorInfo.Name );
    }
}

// <target>
[TestAspect]
internal class TargetCode
{
    private int _field;

    private void M( int i ) { }

    private int P { get; set; }

    private event EventHandler E
    {
        add { }
        remove { }
    }
}
