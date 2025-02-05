using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.GenericAspect_NotSupported;

public class GenericAspect<T> : TypeAspect
{
    public GenericAspect( T defaultValue ) { }

    [Introduce]
    public T? IntroducedProperty { get; set; }
}

// <target>
[GenericAspect<int>( 5 )]
public class C { }