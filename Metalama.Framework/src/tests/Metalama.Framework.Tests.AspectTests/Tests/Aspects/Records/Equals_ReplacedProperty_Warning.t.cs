// Warning LAMA0653 on `Value`: `The original implementation of 'Target.Equals(Target?)' generated for record 'Target' reads the property 'Target.Value', whereas the C# compiler reads its backing field. An aspect overrides the property with a template that does not call the original implementation, so the property has no backing field left and the generated body reads the value that the aspect returns instead of the value that the record stores. Call 'meta.Proceed()' in the template that overrides the property.`
[Override]
internal record Target
{
  public int Value
  {
    get
    {
      return (global::System.Int32)42;
    }
    set
    {
    }
  }
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_ReplacedProperty_Warning.Target? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Value, other.Value));
  }
}