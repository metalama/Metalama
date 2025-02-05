public class TargetClass
{
  public int Field;
  [InvokerAspect]
  public int Invoker
  {
    get
    { // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      // Invoke this.Field
      _ = this.Field;
      return 0;
    }
    set
    { // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
      // Invoke this.Field
      this.Field = 42;
    }
  }
}