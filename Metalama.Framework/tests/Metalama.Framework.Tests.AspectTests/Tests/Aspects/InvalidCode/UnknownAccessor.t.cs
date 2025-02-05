// Syntax tree verification failed.
// Error CS1014 on `getx`: `A get or set accessor expected`
// Error CS1014 on ``: `A get or set accessor expected`
internal class TargetCode
{
  [Aspect]
  public int Foo { getx; private init; }
  [Aspect]
  public int Bar {; private init; }
}