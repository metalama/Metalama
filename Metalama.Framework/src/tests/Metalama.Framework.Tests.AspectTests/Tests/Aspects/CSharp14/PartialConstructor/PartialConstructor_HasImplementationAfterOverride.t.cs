// Hidden CS8019 on `using System;`: `Unnecessary using directive.`
// Warning BUG780C on `C`: `Constructor 'C.C()' HasImplementation: True`
[CheckAspect]
internal partial class C
{
  [OverrideAspect]
  public partial C();
  public partial C()
  {
    global::System.Console.WriteLine("Overridden.");
  }
}
