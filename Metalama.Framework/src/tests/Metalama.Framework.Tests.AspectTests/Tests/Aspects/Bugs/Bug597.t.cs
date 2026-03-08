[MyAspect]
public partial class TargetClass
{
  private void TestTypeOfIntroducedGenericInstance()
  {
    global::System.Console.WriteLine(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug597.TargetClass.IIntroducedInterface<global::System.Object>).Name);
  }
  interface IIntroducedInterface<T>
  {
  }
}
