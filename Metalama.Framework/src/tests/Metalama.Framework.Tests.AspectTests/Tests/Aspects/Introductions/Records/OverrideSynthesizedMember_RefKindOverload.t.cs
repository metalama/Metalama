[Introduction]
public class TargetType
{
  public record Positional(global::System.String Name, global::System.Int32 Count)
  {
    public void Deconstruct(global::System.String name, global::System.Int32 count)
    {
      global::System.Console.WriteLine($"This is the overload that takes its parameters by value: {name}, {count}.");
    }
    public void Deconstruct(out global::System.String Name, out global::System.Int32 Count)
    {
      global::System.Console.WriteLine("This is the override of the synthesized method.");
      Name = this.Name;
      Count = this.Count;
      return;
    }
  }
}