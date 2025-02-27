internal sealed class SomeImplementation : IInterfaceB
{
  [return: global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.InterfaceMember_ReturnValue.MyAttribute]
  public int M(int arg) => arg;
}