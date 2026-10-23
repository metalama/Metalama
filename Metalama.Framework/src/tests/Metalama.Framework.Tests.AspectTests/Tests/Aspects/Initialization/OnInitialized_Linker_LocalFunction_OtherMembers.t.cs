public class Caller
{
  public Caller()
  {
    Document Create() => new Document
    {
      Title = "constructor"
    }.WithInitialize();
    Create();
  }
  public Document Property
  {
    get
    {
      Document Create() => InitializableExtensions.WithInitialize<Document>(new());
      return Create();
    }
  }
  public Document Copy(Document document)
  {
    Document Clone() => (document with
    {
      Title = "copy"
    }
    ).WithInitialize(InitializationMetadata.Modify);
    return Clone();
  }
}