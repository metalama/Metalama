[Introduction]
internal struct TargetStruct
{
  public TargetStruct()
  {
  }
  public int ExistingField = 42;
  public int ExistingProperty { get; set; } = 42;
  public global::System.Int32 IntroducedProperty { get; set; } = default;
  public global::System.Int32 IntroducedProperty_Initializer { get; set; } = (global::System.Int32)42;
  public static global::System.Int32 IntroducedProperty_Static { get; set; } = default;
  public static global::System.Int32 IntroducedProperty_Static_Initializer { get; set; } = (global::System.Int32)42;
}