[Override]
internal record DerivedRecord(int X, int Y) : BaseRecord(X)
{
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    return unchecked(((base.GetHashCode()) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.Y));
  }
}