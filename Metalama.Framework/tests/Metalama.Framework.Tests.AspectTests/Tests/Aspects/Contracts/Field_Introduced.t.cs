[IntroduceAndFilter]
internal class Target
{
  private global::System.String _existingField = default !;
  private global::System.String ExistingField
  {
    get
    {
      var returnValue = this._existingField;
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
      this._existingField = value;
    }
  }
  private global::System.String? _introducedField;
  private global::System.String? IntroducedField
  {
    get
    {
      var returnValue = this._introducedField;
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
      this._introducedField = value;
    }
  }
}