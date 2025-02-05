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
    this.Method_Source<T>(x);
    this.Method_Source<T>(x);
    return;
  }
  private void Method_Source<T>(int x)
  {
  }
  private void IntroducedMethod_Introduction<
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes_Uninlineable.GenericParameterOnlyAttribute]
  T>([global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes_Uninlineable.ParameterOnly] global::System.Int32 x)
  {
  }
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes_Uninlineable.MethodOnlyAttribute]
  [return: global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes_Uninlineable.ReturnValueOnlyAttribute]
  public void IntroducedMethod<
  [global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes_Uninlineable.GenericParameterOnlyAttribute]
  T>([global::Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes_Uninlineable.ParameterOnly] global::System.Int32 x)
  {
    global::System.Console.WriteLine("This is the overridden method.");
    this.IntroducedMethod_Introduction<T>(x);
    this.IntroducedMethod_Introduction<T>(x);
    return;
  }
}