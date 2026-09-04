[ComparisonAttribute]
internal record Target
{
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582.Target? other)
  {
    // <target>
    global::System.Boolean result;
    result = (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract);
    return (global::System.Boolean)result;
  }
}