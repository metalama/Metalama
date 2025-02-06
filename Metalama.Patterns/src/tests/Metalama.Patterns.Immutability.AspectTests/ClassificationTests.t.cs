public class C
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  private class Fabric : TypeFabric
  {
    [Introduce]
    public static void PrintImmutability() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public static void PrintImmutability()
  {
  // C: None
  // ClassMarkedDeeplyImmutable: Deep
  // ClassMarkedNotImmutable: None
  // ClassMarkedShallowlyImmutable: Shallow
  // ClassNotMarked: None
  // ReadOnlyStruct: Shallow
  }
}