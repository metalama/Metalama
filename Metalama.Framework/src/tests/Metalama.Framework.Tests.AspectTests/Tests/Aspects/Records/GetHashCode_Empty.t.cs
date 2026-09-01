[Override]
internal record Target
{
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    return unchecked(global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract));
  }
}