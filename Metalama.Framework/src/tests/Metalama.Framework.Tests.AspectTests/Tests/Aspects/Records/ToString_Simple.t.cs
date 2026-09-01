[Override]
internal record Target(int X, string Y)
{
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