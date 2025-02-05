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