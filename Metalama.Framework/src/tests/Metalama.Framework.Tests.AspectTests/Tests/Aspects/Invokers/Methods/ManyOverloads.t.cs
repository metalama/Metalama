public class TargetClass
{
  [InvokerAspect]
  public void Invoker()
  {
    this.Method(0);
    this.Method((global::System.Int64)0);
    this.Method((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ManyOverloads.A)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ManyOverloads.B());
    this.Method(new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ManyOverloads.B());
    return;
  }
  public void Method(int i)
  {
  }
  public void Method(long i)
  {
  }
  public void Method(A i)
  {
  }
  public void Method(B i)
  {
  }
}