namespace Metalama.Framework.Tests.AspectTests.Tests.Options.BaseType;
[MyOptions("Attribute.C")]
[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Attribute.C")]
public class C
{
}
[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->->Attribute.C")]
public class D : C
{
}