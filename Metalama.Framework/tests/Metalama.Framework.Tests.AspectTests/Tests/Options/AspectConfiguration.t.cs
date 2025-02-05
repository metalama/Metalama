namespace Doc.AspectConfiguration
{
  // Some target code.
  public class SomeClass
  {
    [Log]
    public void SomeMethod()
    {
      global::System.Console.WriteLine("GeneralCategory: Executing SomeClass.SomeMethod().");
      return;
    }
  }
  namespace ChildNamespace
  {
    public class SomeOtherClass
    {
      [Log]
      public void SomeMethod()
      {
        global::System.Console.WriteLine("GeneralCategory: Executing SomeOtherClass.SomeMethod().");
        return;
      }
    }
  }
}