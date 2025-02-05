[Introduction]
internal struct TargetStruct
{
  private int _existingField;
  public TargetStruct(int x)
  {
    _existingField = x;
  }
  public global::System.Int32 IntroducedProperty { get; set; }
  public global::System.Int32 IntroducedProperty_Initializer { get; set; } = (global::System.Int32)42;
  public static global::System.Int32 IntroducedProperty_Static { get; set; }
  public static global::System.Int32 IntroducedProperty_Static_Initializer { get; set; } = (global::System.Int32)42;
}