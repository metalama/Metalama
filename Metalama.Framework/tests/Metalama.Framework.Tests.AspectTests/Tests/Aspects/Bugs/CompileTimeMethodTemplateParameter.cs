using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CompileTimeMethodTemplateParameter;

internal class AspectAttribute : TypeAspect
{
    [Template]
    private void Template1( int a ) => IntMethod( a );

    [CompileTime]
    private int IntMethod( int i ) => i;

    [Template]
    private void Template2( int b ) => VoidMethod( b );

    [CompileTime]
    private void VoidMethod( int j ) { }
}