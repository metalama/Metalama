[Override]
internal record struct Transformed(int X, string Y)
{
  public readonly global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.RunTime_RecordStruct.Transformed other)
  {
    // <target>
    return global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X) && global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.Equals(this.Y, other.Y);
  }
  public override readonly global::System.Int32 GetHashCode()
  {
    // <target>
    return unchecked(((global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.X)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.GetHashCode(this.Y));
  }
  public override readonly global::System.String ToString()
  {
    // <target>
    global::System.Text.StringBuilder __recordStringBuilder = new global::System.Text.StringBuilder();
    __recordStringBuilder.Append("Transformed");
    __recordStringBuilder.Append(" { ");
    if (this.PrintMembers(__recordStringBuilder))
    {
      __recordStringBuilder.Append(' ');
    }
    __recordStringBuilder.Append('}');
    return __recordStringBuilder.ToString();
  }
}