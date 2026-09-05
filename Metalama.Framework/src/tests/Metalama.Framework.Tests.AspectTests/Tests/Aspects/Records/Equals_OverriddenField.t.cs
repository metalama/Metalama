[Override]
internal record Target
{
  private global::System.Int32 _value;
  public global::System.Int32 Value
  {
    get
    {
      global::System.Console.WriteLine("  (the getter of Value runs)");
      return this._value;
    }
    set
    {
      this._value = value;
    }
  }
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_OverriddenField.Target? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this._value, other._value));
  }
}