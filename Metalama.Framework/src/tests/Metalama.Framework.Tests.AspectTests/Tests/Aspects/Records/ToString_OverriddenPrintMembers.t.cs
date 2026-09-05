[Override]
internal record Target(int X)
{
  protected virtual global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    global::System.Boolean result;
    global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
    builder.Append("X = ");
    builder.Append(this.X.ToString());
    result = true;
    builder.Append(", Suffix = 1");
    return (global::System.Boolean)result;
  }
  public override global::System.String ToString()
  {
    // <target>
    return this.ToString_Source();
  }
  private global::System.String ToString_Source()
  {
    global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
    builder.Append("Target");
    builder.Append(" { ");
    if (this.PrintMembers(builder))
    {
      builder.Append(' ');
    }
    builder.Append('}');
    return builder.ToString();
  }
}