[Introduction]
public class TargetType
{
  public record struct PositionalStruct(global::System.Int32 Value)
  {
    private global::System.Int32 _counter;
    public global::System.Int32 MethodTemplate()
    {
      return (global::System.Int32)42;
    }
  }
}