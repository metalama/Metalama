internal class TargetCode
{
  [TestAttribute]
  private int Property
  {
    get
    {
      return this.Property;
    }
    set
    {
      this.Property = value;
    }
  }
}