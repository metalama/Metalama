[AutoIncrementAttribute]
private int Property
{
  get
  {
    if (oldValue != this._property)
    {
      this._property = this._property + 1;
      this._property += 1;
      this._property++;
      ++this._property;
    }
    return this._property;
  }
  set
  {
    throw new global::System.NotImplementedException();
  }
}