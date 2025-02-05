[Introduction]
internal class TargetClass
{
  public global::System.Int32 IntroducedProperty_Accessors
  {
    get
    {
      global::System.Console.WriteLine("Get");
      return (global::System.Int32)42;
    }
    set
    {
      global::System.Console.WriteLine(value);
    }
  }
  public global::System.Int32 IntroducedProperty_Auto { get; set; }
  public global::System.Int32 IntroducedProperty_Auto_GetOnly { get; }
  public global::System.Int32 IntroducedProperty_Auto_GetOnly_Initializer { get; } = (global::System.Int32)42;
  public global::System.Int32 IntroducedProperty_Auto_Initializer { get; set; } = (global::System.Int32)42;
  public static global::System.Int32 IntroducedProperty_Auto_Static { get; set; }
}