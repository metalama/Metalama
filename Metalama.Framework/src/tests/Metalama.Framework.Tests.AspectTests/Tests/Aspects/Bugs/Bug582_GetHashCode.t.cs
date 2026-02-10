[OverrideGetHashCodeAttribute]
internal record Target
{
  public override global::System.Int32 GetHashCode()
  {
    // <target>
    global::System.Int32 result;
    throw new global::System.NotSupportedException("Calling the original implementation of a compiler-synthesized record member is not supported. Do not use meta.Proceed() when overriding synthesized record members like Equals or GetHashCode.");
    return (global::System.Int32)result;
  }
}