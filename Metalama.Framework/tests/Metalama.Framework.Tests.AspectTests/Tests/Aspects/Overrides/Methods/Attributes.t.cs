[Introduction]
[Override]
internal class TargetClass
{
  [MethodOnly]
  [return: ReturnValueOnly]
  public void Method<
  [GenericParameterOnly]
  T>([ParameterOnly] int x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    return;
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes.MethodOnlyAttribute]
  [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes.ReturnValueOnlyAttribute]
  public void IntroducedMethod<
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes.GenericParameterOnlyAttribute]
  T>([global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes.ParameterOnly] global::System.Int32 x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    return;
  }
}