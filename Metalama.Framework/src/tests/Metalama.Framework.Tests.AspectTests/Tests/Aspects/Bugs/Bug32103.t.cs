[MemberCountAspect]
public class TargetClass
{
  public void Method1()
  {
  }
  public void Method1(int a)
  {
  }
  public void Method2()
  {
  }
  public global::System.Collections.Generic.Dictionary<global::System.String, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32103.MethodOverloadCount> GetMethodOverloadCount()
  {
    return (global::System.Collections.Generic.Dictionary<global::System.String, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32103.MethodOverloadCount>)new global::System.Collections.Generic.Dictionary<global::System.String, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32103.MethodOverloadCount>
    {
      {
        "Method1",
        new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32103.MethodOverloadCount("Method1", 2)
      },
      {
        "Method2",
        new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32103.MethodOverloadCount("Method2", 1)
      }
    };
  }
}