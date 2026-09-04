// Warning LAMA0652 on `X`: `The original implementation of 'Target.Equals(Target?)' generated for record 'Target' reads the property 'Target.X', whereas the C# compiler reads its backing field. The backing field of an auto-property cannot be read from source code. The two implementations differ when a derived type overrides the property. Declare the property explicitly with a backing field, or make it non-overridable.`
[Override]
internal record Target
{
  public virtual int X { get; set; }
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_VirtualProperty_Warning.Target? other)
  {
    // <target>
    // The generated body reads the virtual property instead of its backing field, which the C# compiler reads, so the
    // linker reports LAMA0652.
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X));
  }
}