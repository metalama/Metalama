[Aspect]
internal class TargetCode
{
  private global::System.Int32 Add(global::System.Int32 a)
  {
    global::System.Int32 b = Add(1);
    return (global::System.Int32)(1 + b + 1);
    throw new global::System.Exception();
  }
}