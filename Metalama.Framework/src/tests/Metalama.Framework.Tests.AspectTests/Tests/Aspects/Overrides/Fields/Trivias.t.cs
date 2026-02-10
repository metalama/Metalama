internal class TargetClass
{
  private global::System.Int32 _field;
  /// <summary>
  /// A simple field.
  /// </summary>
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias.OverrideAttribute]
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._field = value;
    }
  }
  private global::System.Int32 _fieldWithComment;
  /// <summary>
  /// A field with a regular comment.
  /// </summary>
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias.OverrideAttribute]
  [global::System.ComponentModel.DescriptionAttribute("A described field")]
  public global::System.Int32 FieldWithComment
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._fieldWithComment;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._fieldWithComment = value;
    }
  }
  private static global::System.Int32 _staticField;
  /// <summary>
  /// A static field.
  /// </summary>
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias.OverrideAttribute]
  public static global::System.Int32 StaticField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias.TargetClass._staticField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias.TargetClass._staticField = value;
    }
  }
}
