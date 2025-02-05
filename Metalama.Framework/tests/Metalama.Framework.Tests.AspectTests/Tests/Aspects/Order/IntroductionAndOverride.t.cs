[First]
[Second]
[Third]
internal class TargetClass
{
  public int Method()
  {
    try
    {
      global::System.Console.Write("This is overridden by the first aspect.");
      try
      {
        global::System.Console.Write("This is overridden by the second aspect.");
        try
        {
          global::System.Console.Write("This is overridden by the third aspect.");
          return 42;
        }
        finally
        {
          global::System.Console.Write("This is overridden by the third aspect.");
        }
      }
      finally
      {
        global::System.Console.Write("This is overridden by the second aspect.");
      }
    }
    finally
    {
      global::System.Console.Write("This is overridden by the first aspect.");
    }
  }
  public void IntroducedMethod1()
  {
    global::System.Console.Write("This is introduced by the first aspect.");
  }
  public void IntroducedMethod2()
  {
    try
    {
      global::System.Console.Write("This is overridden by the first aspect.");
      global::System.Console.Write("This is introduced by the second aspect.");
      return;
    }
    finally
    {
      global::System.Console.Write("This is overridden by the first aspect.");
    }
  }
  public void IntroducedMethod3()
  {
    try
    {
      global::System.Console.Write("This is overridden by the first aspect.");
      try
      {
        global::System.Console.Write("This is overridden by the second aspect.");
        global::System.Console.Write("This is introduced by the third aspect.");
      }
      finally
      {
        global::System.Console.Write("This is overridden by the second aspect.");
      }
      return;
    }
    finally
    {
      global::System.Console.Write("This is overridden by the first aspect.");
    }
  }
}