[MyAspect]
public class C(int x, global::System.Int32 p = 15) : A(42)
{
  public int Y { get; } = x;
  public global::System.Int32 BuiltProperty { get; set; } = p;
}