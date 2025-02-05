[Introduction]
internal readonly struct TargetStruct
{
  public readonly int _fieldInitializedByCtor;
  public readonly int _fieldInitializedByExpression = 42;
  public TargetStruct()
  {
    _fieldInitializedByCtor = 42;
  }
  public readonly global::System.Int32 IntroducedField;
  public readonly global::System.Int32 IntroducedField_Initializer = (global::System.Int32)42;
  public static readonly global::System.Int32 IntroducedField_Static;
  public static readonly global::System.Int32 IntroducedField_Static_Initializer = (global::System.Int32)42;
}