#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Annotations.AddAnnotation;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ReadAnnotationAspect), typeof(AddAnnotationAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Annotations.AddAnnotation;

public class AddAnnotationAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddAnnotation( new MyAnnotation( "TheValue" ) );
    }
}

public class ReadAnnotationAspect : TypeAspect
{
    [Introduce]
    public string? TheAnnotation = meta.Target.Type.Enhancements().GetAnnotations<MyAnnotation>().Single().Value;
}

// <target>
[AddAnnotationAspect]
[ReadAnnotationAspect]
internal class C { }