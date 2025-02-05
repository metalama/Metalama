[assembly: MyOptions("FromAssembly")]
[PrintOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject.ActualOptionsAttribute("FromBaseClass")]
internal class DerivedClass : BaseClass
{
}
[PrintOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject.ActualOptionsAttribute("BaseDeclaringClass")]
internal class DerivedOfNested : BaseNestingClass.BaseNestedClass
{
}
[MyOptions("OtherClass")]
internal class OtherClass
{
  [PrintOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject.ActualOptionsAttribute("OtherClass")]
  internal class C
  {
  }
}
[PrintOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject.ActualOptionsAttribute("FromAssembly")]
internal class ClassWithoutOptions
{
}
[PrintOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject.ActualOptionsAttribute("FromDependencyAssembly")]
internal class DerivedFromBaseClassWithoutDirectOptions : BaseClassWithoutDirectOptions
{
}