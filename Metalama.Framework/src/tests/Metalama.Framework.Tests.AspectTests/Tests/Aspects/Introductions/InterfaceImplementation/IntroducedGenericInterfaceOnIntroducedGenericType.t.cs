// Warning MY001 on `TargetType`: `Test<U>.AllImplementedInterfaces: ITest<TargetType.Test<U>/U>`
// Warning MY001 on `TargetType`: `Test<U>.ImplementedInterfaces: ITest<TargetType.Test<U>/U>`
// Warning MY001 on `TargetType`: `Test<int>.AllImplementedInterfaces: ITest<int>`
// Warning MY001 on `TargetType`: `Test<int>.ImplementedInterfaces: ITest<int>`
[IntroductionAttribute]
public class TargetType
{
  interface ITest<T>
  {
  }
  class Test<U> : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IntroducedGenericInterfaceOnIntroducedGenericType.TargetType.ITest<U>
  {
  }
}
