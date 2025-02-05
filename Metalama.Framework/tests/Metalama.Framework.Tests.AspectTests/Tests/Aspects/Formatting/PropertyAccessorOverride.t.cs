public class Target
{
  // Before Foo.
  [Aspect1]
  [Aspect2]
  public int Foo // After Foo name.
  { // After Foo opening brace.
    // Before Foo.get.
    get // After Foo.get keyword.
    // Before Foo.get opening brace.
    { // After Foo.get opening brace.
      // Before Foo.get opening brace.
      // Comment before Aspect1.
      Console.WriteLine("Aspect1");
      // Comment mid Aspect1.
      // Comment before Aspect2.
      Console.WriteLine("Aspect2");
      // Comment mid Aspect2.
      Console.WriteLine("Foo.get");
      return 42;
    // Comment after Aspect2.
    // Comment after Aspect1.
    // After Foo.get closing brace.
    // Before Foo.get closing brace.
    } // After Foo.get closing brace.
    // After Foo.get and before Foo.set.
    set // After Foo.set keyword.
    // Before Foo.set opening brace.
    { // After Foo.set opening brace.
      // Before Foo.set opening brace.
      // Comment before Aspect1.
      Console.WriteLine("Aspect1");
      // Comment mid Aspect1.
      // Comment before Aspect2.
      Console.WriteLine("Aspect2");
      // Comment mid Aspect2.
      Console.WriteLine("Foo.set");
    // Comment after Aspect1.
    // After Foo.set closing brace.
    // Comment after Aspect2.
    // Before Foo.set closing brace.
    } // After Foo.set closing brace.
  // Before Foo closing brace.
  } // After Foo closing brace.
  private int _bar;
  // After Foo/before Bar.
  [Aspect1]
  [Aspect2]
  public int Bar // After Bar name.
  { // After Bar opening brace.
    // Before Bar.get.
    get // After Bar.get keyword.
    // Before Bar.get semicolon
    { // After Bar.get semicolon.
      // Before Bar.get.
      // Comment before Aspect1.
      Console.WriteLine("Aspect1");
      // Comment mid Aspect1.
      // Comment before Aspect2.
      Console.WriteLine("Aspect2");
      // Comment mid Aspect2.
      return _bar;
    // Comment after Aspect2.
    // Comment after Aspect1.
    // After Bar.get semicolon.
    }
    // After Bar.get and before Bar.set.
    set // After Bar.set keyword.
    // Before Bar.set semicolon
    { // After Bar.set semicolon.
      // After Bar.get and before Bar.set.
      // Comment before Aspect1.
      Console.WriteLine("Aspect1");
      // Comment mid Aspect1.
      // Comment before Aspect2.
      Console.WriteLine("Aspect2");
      // Comment mid Aspect2.
      _bar = value;
    // Comment after Aspect1.
    // After Bar.set semicolon.
    // Comment after Aspect2.
    }
  // Before Bar closing brace.
  } // After Bar closing brace.
  // After Bar/before Baz.
  [Aspect1]
  [Aspect2]
  public int Baz // After Baz name.
  { // After Baz opening brace.
    // Before Baz.get.
    get // After Baz.get keyword.
    // Before Baz.get arrow.
    { // After Baz.get arrow.
      // Before Baz.get arrow.
      // Comment before Aspect1.
      Console.WriteLine("Aspect1");
      // Comment mid Aspect1.
      // Comment before Aspect2.
      Console.WriteLine("Aspect2");
      // Comment mid Aspect2.
      // Before Baz.get expression.
      return // Before Baz.get expression.
 42 // After Baz.get expression.
      ; // After Baz.get expression.
    // Comment after Aspect2.
    // Comment after Aspect1.
    // After Baz.get expression.
    // Before Baz.get semicolon.
    } // After Baz.get semicolon.
    // Before Baz.set.
    set // After Baz.set keyword.
    // Before Baz.set arrow.
    { // After Baz.set arrow.
      // Before Baz.set arrow.
      // Comment before Aspect1.
      Console.WriteLine("Aspect1");
      // Comment mid Aspect1.
      // Comment before Aspect2.
      Console.WriteLine("Aspect2");
      // Comment mid Aspect2.
      // Before Baz.set expression.
      Console.WriteLine("Foo.set") // After Baz.set expression.
      ;
    // Comment after Aspect1.
    // After Baz.set expression.
    // Comment after Aspect2.
    // Before Baz.set semicolon.
    } // After Baz.set semicolon.
  // Before Baz closing brace.
  } // After Baz closing brace.
  // After Baz/before Qux.
  [Aspect1]
  [Aspect2]
  public int Qux // After Qux name
  // Before Qux.get arrow.
  { // After Qux.get arrow.
    get
    {
      // Before Qux.get arrow.
      // Comment before Aspect1.
      Console.WriteLine("Aspect1");
      // Comment mid Aspect1.
      // Comment before Aspect2.
      Console.WriteLine("Aspect2");
      // Comment mid Aspect2.
      // Before Qux.get expression.
      return // Before Qux.get expression.
 42 // After Qux.get expression.
      ; // After Qux.get expression.
    // Comment after Aspect2.
    // Comment after Aspect1.
    // After Qux.get expression.
    }
  // Before Qux.get semicolon.
  } // After Qux.get semicolon.
// After Qux.
}