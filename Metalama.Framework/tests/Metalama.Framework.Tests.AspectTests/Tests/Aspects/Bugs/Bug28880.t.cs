internal class TargetCode
{
  [TestMethodAspect]
  private int Method(int a)
  {
    throw new global::System.NotSupportedException();
  }
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28880.TestPropertyAspect]
  private global::System.Int32 field
  {
    get
    {
      throw new global::System.NotSupportedException();
    }
    set
    {
      throw new global::System.NotSupportedException();
    }
  }
  [TestPropertyAspect]
  private int Property
  {
    get
    {
      throw new global::System.NotSupportedException();
    }
    set
    {
      throw new global::System.NotSupportedException();
    }
  }
  private int _property2;
  [TestPropertyAspect2]
  private int Property2
  {
    get
    {
      throw new global::System.NotSupportedException();
    }
    set
    {
      this._property2 = value;
    }
  }
}