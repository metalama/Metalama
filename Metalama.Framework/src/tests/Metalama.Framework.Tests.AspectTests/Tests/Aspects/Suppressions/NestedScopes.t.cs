// Warning CS0219  on `a`: `The variable 'a' is assigned but its value is never used`
// Warning CS0219 on `e`: `The variable 'e' is assigned but its value is never used`
internal class TargetClass
{
  [SuppressWarning]
  private void M2(string m)
  {
    var a = 0;
#pragma warning disable CS0219
    // CS0219 not expected.
    var b = 0;
#pragma warning restore CS0219
    // CS0219 not expected.
    var c = 0;
    return;
  }
  private void M1(string m)
  {
#pragma warning disable CS0219
    // CS0219 not expected.
    var d = 0;
#pragma warning restore CS0219
    // CS0219 expected
    var e = 0;
  }
}