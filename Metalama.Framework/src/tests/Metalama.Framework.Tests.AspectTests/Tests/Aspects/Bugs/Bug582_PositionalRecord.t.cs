[ComparisonAttribute]
internal record Target(int X, string Y)
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582_PositionalRecord.Target? other)
  {
    // <target>
    global::System.Boolean result;
    throw new global::System.NotSupportedException("Calling the original implementation of a compiler-synthesized record member is not supported. Do not use meta.Proceed() when overriding synthesized record members like Equals or GetHashCode.");
    return (global::System.Boolean)result;
  }
}