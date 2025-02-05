[IntroduceAndFilter]
internal class Target
{
  private global::System.String _existingField = default !;
  private global::System.String ExistingField
  {
    get
    {
      global::System.String returnValue;
      returnValue = this._existingField;
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
}