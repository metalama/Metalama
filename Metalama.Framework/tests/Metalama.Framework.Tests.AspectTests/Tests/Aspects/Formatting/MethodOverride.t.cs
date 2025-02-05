public class Target
{
  // Comment before Foo.
  [Aspect1]
  [Aspect2]
  public void Foo()
  // Comment before Foo opening brace.
  { // Comment after Foo opening brace.
    // Comment before Foo opening brace.
    // Comment before Aspect1.
    Console.WriteLine("Aspect1");
    // Comment mid Aspect1.
    // Comment before Aspect2.
    Console.WriteLine("Aspect2");
    // Comment mid Aspect2.
    // Comment inside Foo 1.
    Console.WriteLine("Foo"); // Comment inside Foo 2.
  // Comment after Aspect1.
  // Comment after Foo closing brace.
  // Comment after Aspect2.
  // Comment before Foo closing brace.
  } // Comment after Foo closing brace.
  // Comment after Foo.
  // Comment before Bar.
  [Aspect1]
  [Aspect2]
  public int Bar()
  // Comment before Bar opening brace.
  { // Comment after Bar opening brace.
    // Comment before Bar opening brace.
    // Comment before Aspect1.
    Console.WriteLine("Aspect1");
    // Comment mid Aspect1.
    // Comment before Aspect2.
    Console.WriteLine("Aspect2");
    // Comment mid Aspect2.
    // Comment inside Bar 1.
    Console.WriteLine("Bar"); // Comment inside Bar 2.
    // Comment inside Bar 3.
    return 42; // Comment inside Bar 4.
  // Comment after Aspect2.
  // Comment after Aspect1.
  // Comment after Bar closing brace.
  // Comment before Bar closing brace.
  } // Comment after Bar closing brace.
// Comment after Bar.
}