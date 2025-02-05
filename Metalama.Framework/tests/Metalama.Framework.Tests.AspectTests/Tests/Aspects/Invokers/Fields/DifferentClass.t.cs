public class TargetClass
{
  private DifferentClass? instance;
  [InvokerAspect]
  public int Invoker
  {
    get
    { // Invoke instance.Field
      _ = this.instance.Field;
      // Invoke instance?.Field
      _ = this.instance?.Field;
      // Invoke instance.Field
      _ = this.instance.Field;
      // Invoke instance?.Field
      _ = this.instance?.Field;
      return 0;
    }
    set
    { // Invoke instance.Field
      this.instance.Field = 42;
      // Invoke instance.Field
      this.instance.Field = 42;
    }
  }
}