internal class TargetCode
{
  [Stopwatch]
  private int M(int a, int b)
  {
    var stopWatch = global::System.Diagnostics.Stopwatch.StartNew();
    global::System.Int32 returnValue;
    returnValue = a + b;
    stopWatch.Stop();
    global::System.Diagnostics.Trace.WriteLine(string.Format(format: "Method {0} executed in {1} ms.", arg0: "M", arg1: stopWatch.ElapsedMilliseconds));
    return (global::System.Int32)returnValue;
  }
}