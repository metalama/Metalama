[Override]
internal record struct Target(int X, string Y)
{
  public override readonly global::System.Int32 GetHashCode()
  {
    // <target>
    return unchecked(((global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.X)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.GetHashCode(this.Y));
  }
}