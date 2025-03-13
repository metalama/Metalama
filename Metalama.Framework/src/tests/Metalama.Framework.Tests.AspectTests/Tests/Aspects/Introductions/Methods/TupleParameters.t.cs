[MyAspect]
internal class C
{
  internal void M((global::System.Int32[] A, (global::System.String C, global::System.String[] D, global::System.Int32? [] E) B) x, (global::System.Int32 F, (global::System.Int32? G, global::System.String H)? I)? y)
  {
    global::System.Console.WriteLine($"{x.A}, {x.B.C}, {y.Value.F}");
  }
}