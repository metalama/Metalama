public class TestClass
{
  public TestClass([NotNull][NotEmpty] IReadOnlyCollection<int> list)
  {
    if (list == null !)
    {
      throw new ArgumentNullException("list", "The 'list' parameter must not be null.");
    }
    if (list.Count <= 0)
    {
      throw new ArgumentException("The 'list' parameter must not be null or empty.", "list");
    }
  }
  [return: NotNull]
  [return: NotEmpty]
  public IReadOnlyCollection<int> Foo([NotNull][NotEmpty] IReadOnlyCollection<int> list)
  {
    if (list == null !)
    {
      throw new ArgumentNullException("list", "The 'list' parameter must not be null.");
    }
    if (list.Count <= 0)
    {
      throw new ArgumentException("The 'list' parameter must not be null or empty.", "list");
    }
    IReadOnlyCollection<int> returnValue;
    returnValue = list;
    if (returnValue == null !)
    {
      throw new PostconditionViolationException("The return value must not be null.");
    }
    if (returnValue.Count <= 0)
    {
      throw new PostconditionViolationException("The return value must not be null or empty.");
    }
    return returnValue;
  }
  private IReadOnlyCollection<int> _property = default !;
  [NotNull]
  [NotEmpty]
  public IReadOnlyCollection<int> Property
  {
    get
    {
      return _property;
    }
    set
    {
      if (value == null !)
      {
        throw new ArgumentNullException("value", "The 'Property' property must not be null.");
      }
      if (value.Count <= 0)
      {
        throw new ArgumentException("The 'Property' property must not be null or empty.", "value");
      }
      _property = value;
    }
  }
}