[IntroduceAndFilter]
internal class Target
{
  private string? _existingProperty;
  public string? ExistingProperty
  {
    get
    {
      var returnValue = this._existingProperty;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      this._existingProperty = value;
    }
  }
  private global::System.String? _introducedProperty;
  public global::System.String? IntroducedProperty
  {
    get
    {
      var returnValue = this._introducedProperty;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
    set
    {
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
      this._introducedProperty = value;
    }
  }
}