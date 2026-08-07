namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssembly_OpenGenericTypeWithStructConstraint
{
  public class Derived : OpenBase<int, string>
  {
    public void Introduced()
    {
    }
  }
}
