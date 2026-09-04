[Override]
internal record Target
{
  public int X;
  public event EventHandler? Changed;
  public void Raise() => this.Changed?.Invoke(this, EventArgs.Empty);
  public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_EventField.Target? other)
  {
    // <target>
    return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.X, other.X) && global::System.Collections.Generic.EqualityComparer<global::System.EventHandler?>.Default.Equals(this.Changed, other.Changed));
  }
}