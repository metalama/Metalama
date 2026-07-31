using Metalama.Framework.Aspects;

namespace BlazorApp;

/// <summary>
/// A trivial aspect so that the compilation contains compile-time code, which forces the aspect pipeline to build a
/// compile-time compilation (and thus resolve the compile-time language version) during the Razor
/// <c>RazorCompileComponentDeclaration</c> pass. Uniquely named so its compile-time project is never served from a
/// warm global cache, i.e. the test always exercises the cold path. Regression test for issue #1741.
/// </summary>
public class Log1741Attribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        System.Console.WriteLine( $"Issue1741: entering {meta.Target.Method.Name}" );

        return meta.Proceed();
    }
}
