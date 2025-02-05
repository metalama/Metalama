[InvokerAspect]
public class TargetClass
{
  public int Invoker
  {
    get
    {
      // Invoke instance.Empty_Field
      _ = this.Field_Empty;
      // Invoke instance.Field
      _ = this.Field;
      // Invoke instance.Field
      _ = this.Field;
      return 0;
    }
    set
    { // Invoke instance.Empty_Field
      this.Field_Empty = 42;
      // Invoke instance.Field
      this.Field = 42;
      // Invoke instance.Field
      this.Field = 42;
    }
  }
  private global::System.Int32 Field_Empty
  {
    get => default(global::System.Int32);
    set
    {
    }
  }
  public global::System.Int32 Field;
}