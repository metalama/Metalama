[OverrideGetHashCodeAttribute]
internal record Target
{
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    global::System.Int32 result;
    result = unchecked(global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract));
    return (global::System.Int32)result;
  }
}