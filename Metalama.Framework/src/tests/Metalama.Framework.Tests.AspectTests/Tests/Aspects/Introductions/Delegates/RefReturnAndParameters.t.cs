[Introduction]
public class TargetType
{
  public delegate ref readonly global::System.Int32 RefReadOnlyReturning(global::System.Int32[] storage);
  public delegate ref global::System.Int32 RefReturning(global::System.Int32[] storage);
  public delegate void WithDefaultValue(global::System.Int32 value = 42);
  public delegate void WithRefParameters(ref global::System.Int32 byRef, out global::System.Int32 byOut, in global::System.Int32 byIn);
}