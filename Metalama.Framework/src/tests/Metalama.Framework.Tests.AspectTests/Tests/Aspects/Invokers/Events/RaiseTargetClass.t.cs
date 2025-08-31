public class TargetClass
{
  public event EventHandler Event;
  [InvokerAspect]
  public void Foo()
  {
    // Invoke this.Event
    this.Event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this.Event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this.Event?.Invoke(null, global::System.EventArgs.Empty);
    // Invoke this.Event
    this.Event?.Invoke(null, global::System.EventArgs.Empty);
  }
}