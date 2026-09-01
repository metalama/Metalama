[Override]
internal record struct Target(int X, string Y)
{
  public override readonly global::System.String ToString()
  {
    // <target>
    global::System.Text.StringBuilder __recordStringBuilder = new global::System.Text.StringBuilder();
    __recordStringBuilder.Append("Target");
    __recordStringBuilder.Append(" { ");
    if (this.PrintMembers(__recordStringBuilder))
    {
      __recordStringBuilder.Append(' ');
    }
    __recordStringBuilder.Append('}');
    return __recordStringBuilder.ToString();
  }
}