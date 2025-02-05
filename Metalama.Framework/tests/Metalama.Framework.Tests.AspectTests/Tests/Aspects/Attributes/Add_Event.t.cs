internal class C
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Event.MyAttribute]
  private event Action Event1;
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Event.MyAttribute]
  private event Action Event2;
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Event.MyAttribute]
  private event Action Event3;
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Event.MyAttribute]
  private event Action Event4
  {
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Event.MyAttribute]
    add
    {
    }
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Event.MyAttribute]
    remove
    {
    }
  }
}