[OnPropertyChangedAspect]
internal class Foo
{
  private string _name = null !;
  [ValidationAspect]
  public string Name
  {
    get
    {
      global::System.String returnValue;
      returnValue = this._name;
      if (returnValue is not null)
      {
        throw new global::System.Exception("The property 'Name' must not be set to null!");
      }
      return returnValue;
    }
    set
    {
      if (value is not null)
      {
        throw new global::System.Exception("The property 'Name' must not be set to null!");
      }
      if (this._name == value)
      {
        goto __aspect_return_1;
      }
      OnChanged("Name", this._name, value);
      this._name = value;
      __aspect_return_1:
        ;
    }
  }
  private void OnChanged(global::System.String propertyName, global::System.Object oldValue, global::System.Object newValue)
  {
  }
}