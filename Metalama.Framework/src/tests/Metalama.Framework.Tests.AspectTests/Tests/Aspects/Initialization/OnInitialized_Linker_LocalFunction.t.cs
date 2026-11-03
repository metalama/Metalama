public class Caller
{
  public void Method()
  {
    var direct = new Document("direct").WithInitialize();
    Document ExpressionBodied() => new Document("expression-bodied").WithInitialize();
    Document BlockBodied()
    {
      return new Document("block-bodied").WithInitialize();
    }
    static Document Static() => new Document("static").WithInitialize();
    Func<Document> CreateDelegate() => () => new Document("delegate").WithInitialize();
    Document Outer()
    {
      Document Inner() => new Document("nested").WithInitialize();
      return Inner();
    }
    ExpressionBodied();
    BlockBodied();
    Static();
    CreateDelegate();
    Outer();
  }
}
