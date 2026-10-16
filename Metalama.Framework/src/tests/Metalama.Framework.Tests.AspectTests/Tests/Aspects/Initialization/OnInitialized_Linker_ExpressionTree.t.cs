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
    Func<string, Document> createDelegate = title => new Document(title).WithInitialize();
  }
}
