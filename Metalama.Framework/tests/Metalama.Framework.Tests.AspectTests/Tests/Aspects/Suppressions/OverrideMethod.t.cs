// Warning CS0219  on `a`: `The variable 'a' is assigned but its value is never used`
internal class TargetClass
{
  [SuppressWarning]
  private void M2(string m)
  {
    var a = 0;
    var x = 0;
    return;
  }
  // CS0219 expected
  private void M1(string m)
  {
    var y = 0;
  }
}