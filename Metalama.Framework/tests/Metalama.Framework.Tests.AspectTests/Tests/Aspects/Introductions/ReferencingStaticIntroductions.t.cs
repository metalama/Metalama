[Id]
internal class TargetClass
{
  private static global::System.Int32 _nextId;
  public static global::System.Int32 Id { get; } = (global::System.Int32)global::System.Threading.Interlocked.Increment(ref _nextId);
  public static void Method(global::System.Int32? id)
  {
    if (id == null)
    {
      Method(Id);
    }
  }
}