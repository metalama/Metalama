// Warning CS0219 on `y`: `The variable 'y' is assigned but its value is never used`
internal class TargetClass
{
  [SuppressWarning]
  private void M2(string m)
  {
    var x = 0;
  }
  // CS0219 expected
  private void M1(string m)
  {
    var y = 0;
  }
}