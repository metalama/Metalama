[Override]
internal record Target
{
  private int _value;
  public int Value
  {
    get
    {
      return this._value;
    }
    set
    {
      this._value = value;
    }
  }
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    return unchecked(((global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this._value));
  }
}