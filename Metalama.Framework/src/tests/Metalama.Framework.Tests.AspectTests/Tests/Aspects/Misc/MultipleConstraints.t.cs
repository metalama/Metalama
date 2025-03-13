[MyAspect]
internal class C : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MultipleConstraints.I
{
  public void IM<T1, T2, T3, T4>()
    where T1 : class, new()
    where T2 : global::System.IO.Stream where T3 : unmanaged where T4 : struct
  {
  }
  private void M<T1, T2, T3, T4>()
    where T1 : class, new()
    where T2 : global::System.IO.Stream where T3 : unmanaged where T4 : struct
  {
  }
}