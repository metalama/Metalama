[Introduction]
public class TargetType
{
  public record NonPositional
  {
  }
  public record Positional(global::System.String Name, global::System.Int32 Count)
  {
  }
  public record struct PositionalStruct(global::System.Double X, global::System.Double Y)
  {
  }
  public readonly record struct ReadOnlyStruct(global::System.Int32 Value)
  {
  }
}