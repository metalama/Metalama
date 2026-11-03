// Warning LAMA0656 on `new Document()`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new Document { Title = title }`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new Document( title )`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new Document( title )`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
// Warning LAMA0656 on `new Document( title )`: `The 'Initialize' method of 'Document' is not called after this object creation expression, because the expression is in a lambda expression converted to an expression tree, and an expression tree cannot contain this call.`
public class Caller
{
  private static readonly Expression<Func<Document>> _create = () => new Document();
  private static readonly Func<Document> _createDelegate = () => new Document().WithInitialize();
  public void Method(IQueryable<string> titles)
  {
    Expression<Func<string, Document>> withInitializer = title => new Document
    {
      Title = title
    };
    var documents = titles.Select(title => new Document(title));
    Expression<Func<IEnumerable<string>, IEnumerable<Document>>> nested = list => list.Select(title => new Document(title));
    var query =
      from title in titles
      select new Document(title);
    Func<string, Document> createDelegate = title => new Document(title).WithInitialize();
  }
}