public class TargetClass
{
  public event EventHandler Event;
  [InvokerAspect]
  public void Foo()
  {
    // Raise this.Event
    this.Event?.Invoke((global::System.Object? )null, global::System.EventArgs.Empty);
    // Raise this.Event
    this.Event?.Invoke((global::System.Object? )null, global::System.EventArgs.Empty);
    // Raise this.Event
    this.Event?.Invoke((global::System.Object? )null, global::System.EventArgs.Empty);
    // Raise this.Event
    this.Event?.Invoke((global::System.Object? )null, global::System.EventArgs.Empty);
  }
}