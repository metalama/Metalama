// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Annotations.ReadThroughNullableReference;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof( ReadAnnotationAspect ), typeof( AddAnnotationAspect ) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Annotations.ReadThroughNullableReference;

public class AddAnnotationAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddAnnotation( new MyAnnotation( "TheValue" ) );
    }
}

// The annotation is added to C and is read through the type of each field of D: a non-nullable, a nullable and an oblivious reference to C.
public class ReadAnnotationAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var field in builder.Target.Fields.ToList() )
        {
            var value = ( (INamedType) field.Type ).Enhancements().GetAnnotations<MyAnnotation>().SingleOrDefault()?.Value;

            builder.IntroduceField(
                "Read" + field.Name,
                typeof(string),
                buildField: f => f.InitializerExpression = ExpressionFactory.Literal( value ) );
        }
    }
}

// <target>
[AddAnnotationAspect]
internal class C { }

// <target>
[ReadAnnotationAspect]
internal class D
{
    public C? Nullable;

    public C NonNullable = new();

#nullable disable
    public C Oblivious;
#nullable enable
}
