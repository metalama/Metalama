internal class TargetClass
{
  // Applied on a field.
  private global::System.String _lastName = default !;
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias_LeadingComment.OverrideAttribute]
  public global::System.String LastName
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._lastName;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._lastName = value;
    }
  }
  // First comment.
  // Second comment.
  private global::System.Int32 _multipleComments;
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias_LeadingComment.OverrideAttribute]
  public global::System.Int32 MultipleComments
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._multipleComments;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._multipleComments = value;
    }
  }
  /* Block comment before field. */
  private global::System.Int32 _blockCommentField;
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias_LeadingComment.OverrideAttribute]
  public global::System.Int32 BlockCommentField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._blockCommentField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._blockCommentField = value;
    }
  }
  private global::System.Int32 _docCommentField;
  /// <summary>
  /// XML doc comment on field.
  /// </summary>
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias_LeadingComment.OverrideAttribute]
  public global::System.Int32 DocCommentField
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._docCommentField;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._docCommentField = value;
    }
  }
  // Regular comment before doc.
  private global::System.Int32 _mixedComments;
  /// <summary>
  /// Mixed: regular + doc comment.
  /// </summary>
  [global::Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias_LeadingComment.OverrideAttribute]
  public global::System.Int32 MixedComments
  {
    get
    {
      global::System.Console.WriteLine("This is the overridden getter.");
      return this._mixedComments;
    }
    set
    {
      global::System.Console.WriteLine("This is the overridden setter.");
      this._mixedComments = value;
    }
  }
}
