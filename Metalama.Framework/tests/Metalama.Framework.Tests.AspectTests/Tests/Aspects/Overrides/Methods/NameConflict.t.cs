internal class TargetClass
{
  [InnerOverride]
  [OuterOverride]
  public int TargetMethod_ConflictBetweenOverrides()
  {
    var i_1 = 27;
    var i = 42;
    var j = 42;
    return 42;
  }
  [InnerOverride]
  [OuterOverride]
  public int TargetMethod_ConflictWithParameter(int i)
  {
    var i_2 = 27;
    var i_1 = 42;
    var j = 42;
    return 42;
  }
  [InnerOverride]
  [OuterOverride]
  public int TargetMethod_ConflictWithTarget()
  {
    var i_2 = 27;
    var i_1 = 42;
    var j = 42;
    var i = 0;
    return 42;
  }
  [InnerOverride]
  [OuterOverride]
  public int TargetMethod_MultipleConflicts()
  {
    var i_2 = 27;
    var i_1 = 42;
    var j_1 = 42;
    var i = 0;
    var j = 0;
    return 42;
  }
}