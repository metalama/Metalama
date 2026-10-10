[TheAspect]
internal class D : C
{
  public override global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("TheAspect.Property");
      return base.Property;
    }
    set
    {
      global::System.Console.WriteLine("TheAspect.Property");
      base.Property = value;
    }
  }
}