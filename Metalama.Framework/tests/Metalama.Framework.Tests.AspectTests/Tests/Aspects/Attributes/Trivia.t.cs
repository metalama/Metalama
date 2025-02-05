internal class Target
{
  [IntroduceAttributeAspect]
  private class IntroduceTarget
  {
    /// <summary>
    /// Gets or sets a test property value.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public static object? TestProperty { get; set; }
    /// <summary>
    /// A test method.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public static void TestMethod()
    {
    }
    // nested class
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private class Nested
    {
    }
    /// <summary>
    /// Field.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _field;
    // multifield
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _f1;
    // multifield
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _f2;
    /// <summary>
    /// Event.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public event EventHandler? Event;
    /// <summary>
    /// Another event.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public event EventHandler? Event2
    {
      add
      {
      }
      remove
      {
      }
    }
  }
  [RemoveAttributeAspect]
  private class RemoveTarget
  {
    /// <summary>
    /// Gets or sets a test property value.
    /// </summary>
    public static object? TestProperty { get; set; }
    /// <summary>
    /// A test method.
    /// </summary>
    public static void TestMethod()
    {
    }
    // nested class
    private class Nested
    {
    }
    /// <summary>
    /// Field.
    /// </summary>
    private int _field;
    // multifield
    private int _f1;
    // multifield
    private int _f2;
    /// <summary>
    /// Event.
    /// </summary>
    public event EventHandler? Event;
    /// <summary>
    /// Another event.
    /// </summary>
    public event EventHandler? Event2
    {
      add
      {
      }
      remove
      {
      }
    }
  }
  [RemoveAttributeAspect]
  [IntroduceAttributeAspect]
  private class ReplaceTarget
  {
    /// <summary>
    /// Gets or sets a test property value.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public static object? TestProperty { get; set; }
    /// <summary>
    /// A test method.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public static void TestMethod()
    {
    }
    // nested class
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private class Nested
    {
    }
    /// <summary>
    /// Field.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _field;
    // multifield
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _f1;
    // multifield
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _f2;
    /// <summary>
    /// Event.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public event EventHandler? Event;
    /// <summary>
    /// Another event.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public event EventHandler? Event2
    {
      add
      {
      }
      remove
      {
      }
    }
  }
  [RemoveAttributeAspect2]
  [IntroduceAttributeAspect]
  private class ReplaceTarget2
  {
    /// <summary>
    /// Gets or sets a test property value.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public static object? TestProperty { get; set; }
    /// <summary>
    /// A test method.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public static void TestMethod()
    {
    }
    // nested class
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private class Nested
    {
    }
    /// <summary>
    /// Field.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _field;
    // multifield
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _f1;
    // multifield
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    private int _f2;
    /// <summary>
    /// Event.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public event EventHandler? Event;
    /// <summary>
    /// Another event.
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia.NewAttribute]
    public event EventHandler? Event2
    {
      add
      {
      }
      remove
      {
      }
    }
  }
}