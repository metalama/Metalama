// Error CS0246 on `T`: `The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)`
// Error CS0246 on `T`: `The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)`
// Error CS0246 on `T`: `The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)`
public class TargetClass
{
  [TestAspect]
  public void Foo()
  {
    this.Bar<T>((T)42, (T)"hello");
    return;
  }
  public void Bar<T>(T first, T second)
  {
  }
}
