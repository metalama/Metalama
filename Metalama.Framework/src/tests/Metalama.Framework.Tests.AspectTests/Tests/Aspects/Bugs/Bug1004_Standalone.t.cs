// Final Compilation.Emit failed.
// Error CS0246 on `Bug1004StandaloneGlobalUsing`: `The type or namespace name 'Bug1004StandaloneGlobalUsing' could not be found (are you missing a using directive or an assembly reference?)`
[SomeAspect]
public class Test
{
  private Bug1004StandaloneGlobalUsing x;
  public void MyMethod()
  {
  }
}