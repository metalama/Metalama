[Override]
internal record Transformed(int X)
{
  public void Deconstruct(out global::System.Int32 X)
  {
    // <target>
    X = default;
    X = this.X;
  }
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.RunTime_OverriddenProperty.Transformed? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this._x, other._x));
  }
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    return unchecked(((global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this._x));
  }
  protected virtual global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
  {
    // <target>
    global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
    builder.Append("X = ");
    builder.Append(this.X.ToString());
    return true;
  }
  public override global::System.String ToString()
  {
    // <target>
    return this.ToString_Source();
  }
  private global::System.String ToString_Source()
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
  private readonly int _x = X;
  public int X
  {
    get
    {
      global::System.Console.WriteLine("  (the getter of X runs)");
      return this._x;
    }
    init
    {
      this._x = value;
    }
  }
}