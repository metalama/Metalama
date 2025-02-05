internal class Targets
{
  private class NaturallyCloneable : ICloneable
  {
    public object Clone()
    {
      return new NaturallyCloneable();
    }
  }
  [DeepClone]
  [TestAspect]
  private class BaseClass : global::System.ICloneable
  {
    private int a;
    private NaturallyCloneable? b;
    public virtual global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.Bug28810.Targets.BaseClass Clone()
    {
      return null;
    }
    public void Foo()
    {
      this.Clone();
      this.Clone();
    }
    global::System.Object global::System.ICloneable.Clone()
    {
      return (global::System.Object)this.Clone();
    }
  }
}