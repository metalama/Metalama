[Introduction]
public class TargetType
{
  public record Positional(global::System.String Name, global::System.Int32 Count)
  {
    public void Deconstruct(out global::System.String Name, out global::System.Int32 Count)
    {
      global::System.Console.WriteLine("Deconstructing.");
      Name = this.Name;
      Count = this.Count;
      return;
    }
    public virtual global::System.Boolean Equals(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.OverrideSynthesizedMember.TargetType.Positional? other)
    {
      global::System.Console.WriteLine("Comparing.");
      return (object)this == (object? )other || ((object? )other != null && this.EqualityContract == other.EqualityContract && global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.Equals(this.Name, other.Name) && global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.Equals(this.Count, other.Count));
    }
    public override global::System.Int32 GetHashCode()
    {
      return (global::System.Int32)(this.GetHashCode_Source() + 1);
    }
    private global::System.Int32 GetHashCode_Source()
    {
      return unchecked(((((global::System.Collections.Generic.EqualityComparer<global::System.Type>.Default.GetHashCode(this.EqualityContract)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.String>.Default.GetHashCode(this.Name)) * -1521134295) + global::System.Collections.Generic.EqualityComparer<global::System.Int32>.Default.GetHashCode(this.Count));
    }
    protected virtual global::System.Boolean PrintMembers(global::System.Text.StringBuilder builder)
    {
      global::System.Boolean result;
      global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
      builder.Append("Name = ");
      builder.Append((object)this.Name);
      builder.Append(", Count = ");
      builder.Append(this.Count.ToString());
      result = true;
      builder.Append(", Suffix = 1");
      return (global::System.Boolean)result;
    }
    public override global::System.String ToString()
    {
      global::System.Console.WriteLine("Formatting.");
      return this.ToString_Source();
    }
    private global::System.String ToString_Source()
    {
      global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
      builder.Append("Positional");
      builder.Append(" { ");
      if (this.PrintMembers(builder))
      {
        builder.Append(' ');
      }
      builder.Append('}');
      return builder.ToString();
    }
    protected virtual global::System.Type EqualityContract
    {
      get
      {
        global::System.Console.WriteLine("Reading the equality contract.");
        return typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.OverrideSynthesizedMember.TargetType.Positional);
      }
    }
  }
}