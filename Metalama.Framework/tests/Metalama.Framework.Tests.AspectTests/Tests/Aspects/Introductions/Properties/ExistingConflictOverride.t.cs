[Introduction]
internal class TargetClass : BaseClass
{
  public int ExistingProperty
  {
    get
    {
      global::System.Console.WriteLine("This is introduced property.");
      return 27;
    }
  }
  public static int ExistingProperty_Static
  {
    get
    {
      global::System.Console.WriteLine("This is introduced property.");
      return 27;
    }
  }
  public override global::System.Int32 ExistingBaseProperty
  {
    get
    {
      global::System.Console.WriteLine("This is introduced property.");
      return base.ExistingBaseProperty;
    }
  }
  public global::System.Int32 NotExistingProperty
  {
    get
    {
      global::System.Console.WriteLine("This is introduced property.");
      return default(global::System.Int32);
    }
  }
  public static global::System.Int32 NotExistingProperty_Static
  {
    get
    {
      global::System.Console.WriteLine("This is introduced property.");
      return default(global::System.Int32);
    }
  }
}