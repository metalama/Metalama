[Introduction]
public class TargetType
{
  public record Positional(global::System.Collections.Generic.List<global::System.Int32> Values)
  {
    public void Deconstruct(out global::System.Collections.Generic.List<global::System.Int32> Values)
    {
      Values = default!;
      Values = this.Values;
    }
    protected virtual global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
    {
      global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
      builder.Append("Values = ");
      builder.Append((object)this.Values);
      return true;
    }
  }
}