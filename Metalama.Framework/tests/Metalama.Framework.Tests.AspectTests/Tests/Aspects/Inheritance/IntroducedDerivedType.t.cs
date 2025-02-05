[IntroduceClassAspect]
public class Targets
{
  [IntroduceMethodInheritableAspect]
  public class BaseType
  {
    public virtual global::System.Int32 Foo()
    {
      global::System.Console.WriteLine("Introduced!");
      return default(global::System.Int32);
    }
  }
  public class ManualDerived : BaseType
  {
    public override global::System.Int32 Foo()
    {
      global::System.Console.WriteLine("Introduced!");
      return base.Foo();
    }
  }
  class IntroducedDerived : global::Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.IntroducedDerivedType.Targets.BaseType
  {
    public override global::System.Int32 Foo()
    {
      global::System.Console.WriteLine("Introduced!");
      return base.Foo();
    }
  }
}