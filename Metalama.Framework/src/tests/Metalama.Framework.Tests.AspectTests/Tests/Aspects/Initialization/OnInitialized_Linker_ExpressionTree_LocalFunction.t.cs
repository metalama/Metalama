// Warning LAMA0656 on `new Document( "local" )`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new Document( "static local" )`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new Document( "nested local" )`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
public class Caller
{
  public Expression<Func<Document>>[] Method()
  {
    return[Create(), CreateStatic(), CreateOuter()];
    Expression<Func<Document>> Create() => () => new Document("local");
    static Expression<Func<Document>> CreateStatic() => () => new Document("static local");
    Expression<Func<Document>> CreateOuter()
    {
      Expression<Func<Document>> CreateInner() => () => new Document("nested local");
      return CreateInner();
    }
  }
}