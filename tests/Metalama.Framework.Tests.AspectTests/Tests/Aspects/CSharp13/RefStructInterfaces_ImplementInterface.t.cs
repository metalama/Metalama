[TheAspect]
ref struct S : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.RefStructInterfaces_ImplementInterface.I
{
  public void M<T>()
    where T : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.RefStructInterfaces_ImplementInterface.I, new(), allows ref struct
  {
    var x = new T();
    x.M<T>();
  }
}