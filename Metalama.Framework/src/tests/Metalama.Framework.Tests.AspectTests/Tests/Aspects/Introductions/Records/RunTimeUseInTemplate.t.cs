[Introduction]
public class TargetType
{
  public void MethodTemplate()
  {
    global::System.Console.WriteLine(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.RunTimeUseInTemplate.TargetType.Level));
    global::System.Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.RunTimeUseInTemplate.TargetType.Level.High);
    var snapshot = new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.RunTimeUseInTemplate.TargetType.Snapshot(1);
    global::System.Console.WriteLine(snapshot);
    global::System.Console.WriteLine(new Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.RunTimeUseInTemplate.TargetType.Snapshot(1)with { Value = 2 });
    global::System.Console.WriteLine(new Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.RunTimeUseInTemplate.TargetType.Handler(x => x + 1));
  }
  public delegate global::System.Int32 Handler(global::System.Int32 value);
  public enum Level
  {
    None,
    High = 10
  }
  public record Snapshot(global::System.Int32 Value)
  {
  }
}