[Introduction]
public class TargetType
{
  public record Positional
  {
    public global::System.Int32 Value { get; init; }
    public void Deconstruct(out global::System.Int32 Value)
    {
      Value = this.Value;
    }
    public Positional(global::System.Int32 Value)
    {
      this.Value = Value;
      global::System.Console.WriteLine("Constructing Positional.");
    }
  }
}