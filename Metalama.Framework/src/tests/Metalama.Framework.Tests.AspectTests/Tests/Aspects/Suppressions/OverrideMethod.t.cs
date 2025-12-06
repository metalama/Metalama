// Warning CS0219 on `y`: `The variable 'y' is assigned but its value is never used`
internal class TargetClass
{
  [SuppressWarning]
  private void M2(string m)
  {
    // CS0219 NOT expected because diagnostics from generated code should be suppressed.
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