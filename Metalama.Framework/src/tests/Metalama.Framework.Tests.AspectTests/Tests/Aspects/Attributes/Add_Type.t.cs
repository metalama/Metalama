internal class TargetClass
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal class C
  {
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal class D : C
  {
  }
}
internal struct TargetStruct
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal struct C
  {
  }
}
internal record TargetRecord
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal record C
  {
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal record D : C
  {
  }
}
internal record class TargetRecordClass
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal record class C
  {
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal record class D : C
  {
  }
}
internal interface TargetInterface
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal interface C
  {
  }
}
internal class TargetEnum
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal enum E
  {
  }
}
internal class TargetDelegate
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Type.MyAttribute]
  internal delegate void D();
}