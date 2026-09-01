[Override]
internal record Target(int X)
{
  public override global::System.String ToString()
  {
    // <target>
    global::System.Console.WriteLine("Overridden!");
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