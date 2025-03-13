// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Annotations.CrossProject;

public class AddAnnotationAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddAnnotation( new MyAnnotation( "TheValue" ), true );
    }
}

public class MyAnnotation : IAnnotation<INamedType>
{
    public MyAnnotation( string? value )
    {
        Value = value;
    }

    public string? Value { get; }
}

[AddAnnotationAspect]
public class BaseClass { }