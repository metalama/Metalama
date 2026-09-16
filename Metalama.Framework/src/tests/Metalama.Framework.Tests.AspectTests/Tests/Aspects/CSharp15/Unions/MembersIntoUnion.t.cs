[Introduction]
public class TargetType
{
  public union Result(global::System.Int32)
  {
    public global::System.Int32 PropertyTemplate
    {
      get
      {
        return (global::System.Int32)42;
      }
      set
      {
      }
    }
    public global::System.Int32 MethodTemplate()
    {
      return (global::System.Int32)42;
    }
    public event global::System.EventHandler? ExplicitEvent
    {
      add
      {
      }
      remove
      {
      }
    }
    class Nested
    {
    }
  }
}