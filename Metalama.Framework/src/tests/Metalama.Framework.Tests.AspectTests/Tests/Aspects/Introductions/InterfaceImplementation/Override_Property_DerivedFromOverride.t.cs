[TheAspect]
internal class C : B
{
  public override global::System.Int32 Property
  {
    get
    {
      global::System.Console.WriteLine("TheAspect.Property.get");
      return base.Property;
    }
    set
    {
      global::System.Console.WriteLine("TheAspect.Property.set");
      base.Property = value;
    }
  }
}