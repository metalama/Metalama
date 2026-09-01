[Override]
internal record struct Target(int X, string Y)
{
  public override readonly global::System.String ToString()
  {
    // <target>
    return (global::System.String)this.ToString_Source()!;
  }
  private readonly global::System.String ToString_Source()
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