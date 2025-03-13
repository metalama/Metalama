public partial class TargetClass
{
  [TestAspect]
  public object M1()
  {
    global::System.Console.WriteLine("object is not Metalama.Framework.Tests.Integration.Tests.Aspects.Bugs.Bug35772.A`1[T]");
    return new object ();
  }
  [TestAspect]
  public A<E> M2()
  {
    global::System.Console.WriteLine("A<E> is Metalama.Framework.Tests.Integration.Tests.Aspects.Bugs.Bug35772.A`1[T]");
    return new A<E>();
  }
  [TestAspect]
  public B<E> M3()
  {
    global::System.Console.WriteLine("B<E> is Metalama.Framework.Tests.Integration.Tests.Aspects.Bugs.Bug35772.A`1[T]");
    return new B<E>();
  }
  [TestAspect]
  public C<E> M4()
  {
    global::System.Console.WriteLine("C<E> is not Metalama.Framework.Tests.Integration.Tests.Aspects.Bugs.Bug35772.A`1[T]");
    return new C<E>();
  }
}