[Introduction]
public class TargetType
{
  public void MethodTemplate()
  {
    global::System.Console.WriteLine("Level");
    global::System.Console.WriteLine(2);
  }
  public delegate void Handler(global::System.Int32 value);
  public enum Level
  {
    None,
    High = 10
  }
}