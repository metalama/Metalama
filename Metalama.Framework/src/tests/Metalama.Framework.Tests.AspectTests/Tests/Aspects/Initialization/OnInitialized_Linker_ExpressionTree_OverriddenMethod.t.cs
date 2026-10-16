// Warning LAMA0656 on `new TargetCode { Value = 1 }`: `The 'Initialize' method of 'TargetCode' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new TargetCode()`: `The 'Initialize' method of 'TargetCode' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new HandWritten()`: `The 'Initialize' method of 'HandWritten' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
public class Caller
{
  [LogAspect]
  public Expression<Func<TargetCode>> Inlined()
  {
    Console.WriteLine("Calling Inlined");
    return () => new TargetCode
    {
      Value = 1
    };
  }
  [ProceedTwiceAspect]
  public Expression<Func<TargetCode>> NotInlined()
  {
    if (Environment.ProcessorCount > 0)
    {
      return this.NotInlined_Source();
    }
    return NotInlined_Source();
  }
  private Expression<Func<TargetCode>> NotInlined_Source()
  {
    return () => new TargetCode();
  }
  public Expression<Func<HandWritten>> HandWrittenInitializable()
  {
    return () => new HandWritten();
  }
}