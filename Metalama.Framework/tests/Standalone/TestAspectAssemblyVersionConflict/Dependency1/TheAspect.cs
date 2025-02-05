using Metalama.Framework.Aspects;
using System.Reflection;

[assembly: AssemblyVersion("1.0.0.0")]

namespace Dependency;


[Inheritable]
public class TheAspect : TypeAspect
{
    [Introduce(WhenExists = OverrideStrategy.New)]
    public void IntroducedMethod1() { }
}