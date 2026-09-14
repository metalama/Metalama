[Introduction]
public class TargetType
{
  public record NonPositional
  {
    public NonPositional()
    {
      global::System.Console.WriteLine("Initializing NonPositional.");
    }
  }
}