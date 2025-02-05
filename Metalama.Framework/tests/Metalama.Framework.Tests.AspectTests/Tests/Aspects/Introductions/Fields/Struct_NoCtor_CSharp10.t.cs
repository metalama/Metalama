[Introduction]
internal struct TargetStruct
{
  public int ExistingField;
  public int ExistingProperty { get; set; }
  public global::System.Int32 IntroducedField = default;
  public global::System.Int32 IntroducedField_Initializer = (global::System.Int32)42;
  public static global::System.Int32 IntroducedField_Static = default;
  public static global::System.Int32 IntroducedField_Static_Initializer = (global::System.Int32)42;
  public TargetStruct()
  {
    this = default;
  }
}