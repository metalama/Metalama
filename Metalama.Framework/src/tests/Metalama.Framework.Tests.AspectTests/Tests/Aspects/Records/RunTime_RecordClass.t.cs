[Override]
internal record Transformed(int X, string Y)
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.RunTime_RecordClass.Transformed? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X) && global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.Equals(this.Y, other.Y));
  }
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    return unchecked(((((global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.X)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.GetHashCode(this.Y));
  }
  protected virtual global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
    builder.Append("X = ");
    builder.Append(this.X.ToString());
    builder.Append(", Y = ");
    builder.Append((object)this.Y);
    return true;
  }
  public override global::System.String ToString()
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