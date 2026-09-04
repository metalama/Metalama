[Override]
internal record DerivedRecord(int X, int Y) : BaseRecord(X)
{
  protected override global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    if (base.PrintMembers(builder))
    {
      builder.Append(", ");
    }
    builder.Append("Y = ");
    builder.Append(this.Y.ToString());
    return true;
  }
}