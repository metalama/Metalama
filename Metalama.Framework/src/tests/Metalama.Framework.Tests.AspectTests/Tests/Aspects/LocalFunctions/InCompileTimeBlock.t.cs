[Aspect]
class Target
{
  public void ConditionFalse()
  {
  }
  public void ConditionTrue()
  {
    LocalFunction();
    void LocalFunction()
    {
    }
  }
  public global::System.Int32 Loop()
  {
    int sum = 0;
    sum += LocalFunction();
    int LocalFunction() => 0;
    sum += LocalFunction_1();
    int LocalFunction_1() => 1;
    sum += LocalFunction_2();
    int LocalFunction_2() => 2;
    return (global::System.Int32)sum;
  }
}