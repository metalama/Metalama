internal class TargetClass
{
  private global::System.String _lastName = default !;
  // Applied on a field.
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
  private global::System.Int32 _multipleComments;
  // First comment.
  // Second comment.
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
  private global::System.Int32 _blockCommentField;
  /* Block comment before field. */
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
}
