[Introduction]
internal struct TargetStruct
{
  private int _existingField;
  public TargetStruct(int x)
  {
    _existingField = x;
  }
  public global::System.Int32 IntroducedField;
  public global::System.Int32 IntroducedField_Initializer = (global::System.Int32)42;
  public static global::System.Int32 IntroducedField_Static;
  public static global::System.Int32 IntroducedField_Static_Initializer = (global::System.Int32)42;
}