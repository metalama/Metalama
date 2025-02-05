using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Annotations.CrossProject;

public class ReadAnnotationAspect : TypeAspect
{
    [Introduce]
    public string? TheAnnotation = meta.Target.Type.BaseType!.Enhancements().GetAnnotations<MyAnnotation>().SingleOrDefault()?.Value;
}

// <target>
[ReadAnnotationAspect]
public class DerivedClass : BaseClass { }