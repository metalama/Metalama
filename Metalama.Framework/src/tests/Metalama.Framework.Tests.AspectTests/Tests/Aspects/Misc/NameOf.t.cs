internal class C
{
  [return: MyAspect]
  public int M([MyAspect] int p)
  {
    global::System.Console.WriteLine("From template run-time: " + string.Join(", ", nameof(p), "MyAspect", "DateTime", "C", "UtcNow"));
    global::System.Console.WriteLine("From template compile-time: p, MyAspect, DateTime, C, UtcNow");
    global::System.Console.WriteLine("From BuildAspect: MyAspect, DateTime, C, UtcNow");
    global::System.Int32 returnValue;
    returnValue = 0;
    global::System.Console.WriteLine("From template run-time: " + string.Join(", ", nameof(returnValue), "MyAspect", "DateTime", "C", "UtcNow"));
    global::System.Console.WriteLine("From template compile-time: returnValue, MyAspect, DateTime, C, UtcNow");
    global::System.Console.WriteLine("From BuildAspect: MyAspect, DateTime, C, UtcNow");
    return returnValue;
  }
}