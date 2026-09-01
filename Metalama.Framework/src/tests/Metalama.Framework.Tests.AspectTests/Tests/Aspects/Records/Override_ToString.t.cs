[Override]
internal record Target(int X)
{
  public override global::System.String ToString()
  {
    // <target>
    global::System.Console.WriteLine("Overridden!");
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