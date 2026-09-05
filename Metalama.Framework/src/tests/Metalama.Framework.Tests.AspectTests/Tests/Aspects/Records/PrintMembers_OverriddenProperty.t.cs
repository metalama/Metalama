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
  protected virtual global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
    builder.Append("Value = ");
    builder.Append(this.Value.ToString());
    return true;
  }
}