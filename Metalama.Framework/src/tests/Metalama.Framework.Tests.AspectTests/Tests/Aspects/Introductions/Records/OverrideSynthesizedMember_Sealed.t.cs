[Introduction]
public class TargetType
{
  public sealed record SealedClass(global::System.Int32 Value)
  {
    private global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
    {
      global::System.Boolean result;
      global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
      builder.Append("Value = ");
      builder.Append(this.Value.ToString());
      result = true;
      builder.Append(", Suffix = 1");
      return (global::System.Boolean)result;
    }
  }
  public record struct Struct(global::System.Int32 Value)
  {
    private readonly global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
    {
      global::System.Boolean result;
      builder.Append("Value = ");
      builder.Append(this.Value.ToString());
      result = true;
      builder.Append(", Suffix = 1");
      return (global::System.Boolean)result;
    }
  }
}