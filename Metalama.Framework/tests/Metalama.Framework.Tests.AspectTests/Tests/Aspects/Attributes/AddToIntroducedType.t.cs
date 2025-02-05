[TypeIntroducingAspect]
internal class C
{
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType.MyAttribute]
  class TestType
  {
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType.MyAttribute]
    private global::System.Int32 _field;
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType.MyAttribute]
    private global::System.String? Property { get; set; }
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType.MyAttribute]
    [return: global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType.MyAttribute]
    private global::System.Int64 Method([global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType.MyAttribute] global::System.String p)
    {
      return (global::System.Int64)0;
    }
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.AddToIntroducedType.MyAttribute]
    public event global::System.EventHandler? Event;
  }
}