[IntroducingAspect]
[AddAttributeAspect]
internal class C
{
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroduced.MyAttribute]
  private global::System.Int32 _field;
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroduced.MyAttribute]
  private global::System.String? Property { get; set; }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroduced.MyAttribute]
  [return: global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroduced.MyAttribute]
  private global::System.Int64 Method([global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroduced.MyAttribute] global::System.String p)
  {
    return (global::System.Int64)0;
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroduced.MyAttribute]
  public event global::System.EventHandler? Event;
}