internal partial class TestTypes
{
  /// <summary>
  /// </summary>
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
  private class C
  {
    /// <summary>
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Field1;
    /// <summary>
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Field2;
    /// <summary>
    /// </summary>
    [Existing]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Field3;
    /// <summary>
    /// </summary>
    [Existing]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Field4;
    /// <summary>
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Property1 { get; }
    /// <summary>
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Property2 { get; }
    /// <summary>
    /// </summary>
    [Existing]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Property3 { get; }
    /// <summary>
    /// </summary>
    [Existing]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    public int? Property4 { get; }
  }
  /// <summary>
  /// </summary>
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
  private enum E
  {
    /// <summary>
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    Value1,
    /// <summary>
    /// </summary>
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    Value2,
    /// <summary>
    /// </summary>
    [Existing]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    Value3,
    /// <summary>
    /// </summary>
    [Existing]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment.TestAttribute]
    Value4
  }
}