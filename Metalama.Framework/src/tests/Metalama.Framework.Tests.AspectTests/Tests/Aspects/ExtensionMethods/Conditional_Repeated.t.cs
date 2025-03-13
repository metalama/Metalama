[ReturnNumbers]
private object Method()
{
  global::System.Collections.Generic.IEnumerable<global::System.Object>? numbers = new object[]
  {
    42
  };
  numbers = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Conditional_Repeated.MyExtensionMethods.MyToList(numbers);
  numbers = global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Conditional_Repeated.MyExtensionMethods.MyToList(numbers);
  return (global::System.Object)numbers;
}