// Warning LAMA0654 on `Offset`: `The original implementation of 'Target.Equals(Target?)' generated for record 'Target' reads the property 'Target.Offset', whereas the C# compiler reads its backing field. The property is declared with the 'field' keyword and its getter has a body, so the getter can return a value other than the one that the backing field holds, and the two implementations then differ. Declare the property explicitly with a backing field, or give it an automatic getter.`
[Override]
internal record Target
{
  public int Offset { get => field + 1; set => field = value; }
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_SemiAutoProperty_Warning.Target? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Offset, other.Offset));
  }
}