[MyAspect]
public class C<T>
{
  public static void Method()
  {
  }
  public void Introduced()
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.C<T>.Method();
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.C<global::System.Int32>.Method();
  }
}