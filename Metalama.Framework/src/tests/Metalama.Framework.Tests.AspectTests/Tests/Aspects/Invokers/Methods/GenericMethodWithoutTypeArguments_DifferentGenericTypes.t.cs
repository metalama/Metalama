// Error CS0246 on `T`: `The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)`
// Error CS0030 on `(global::System.Collections.Generic.List<T>)new global::System.Collections.Generic.HashSet<global::System.Int32>()`: `Cannot convert type 'System.Collections.Generic.HashSet<int>' to 'System.Collections.Generic.List<T>'`
// Error CS0246 on `T`: `The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)`
public class TargetClass
{
  [TestAspect]
  public void Foo()
  {
    this.Bar<T>((global::System.Collections.Generic.List<T>)new global::System.Collections.Generic.HashSet<global::System.Int32>());
    return;
  }
  public void Bar<T>(List<T> items)
  {
  }
}
