[Override]
internal record Target(int X, string Y)
{
  public int[]? Array;
  protected virtual global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
    builder.Append("X = ");
    builder.Append(this.X.ToString());
    builder.Append(", Y = ");
    builder.Append((object)this.Y);
    builder.Append(", Array = ");
    builder.Append((object)this.Array);
    return true;
  }
}