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
    return (global::System.String)this.ToString_Source()!;
  }
  private readonly global::System.String ToString_Source()
  {
    global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
    builder.Append("Transformed");
    builder.Append(" { ");
    if (this.PrintMembers(builder))
    {
      builder.Append(' ');
    }
    builder.Append('}');
    return builder.ToString();
  }
}