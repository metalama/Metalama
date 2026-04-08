public class TestClass
{
  // [Required] on string parameter should NOT trigger LAMA5003 because [Required]
  // checks for empty/whitespace strings, which [NotNull] does not.
  public void PrintString([Required] string foo)
  {
    if (string.IsNullOrWhiteSpace(foo))
    {
      if (foo == null !)
      {
        throw new ArgumentNullException("foo", "The 'foo' parameter is required.");
      }
      else
      {
        throw new ArgumentException("The 'foo' parameter is required.", "foo");
      }
    }
  }
  private string _name = default !;
  // [Required] on string property should also be silently skipped.
  [Required]
  public string Name
  {
    get
    {
      return _name;
    }
    set
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        if (value == null !)
        {
          throw new ArgumentNullException("value", "The 'Name' property is required.");
        }
        else
        {
          throw new ArgumentException("The 'Name' property is required.", "value");
        }
      }
      _name = value;
    }
  }
  // [Required] on string field.
  private string _title = default !;
  [Required]
  public string Title
  {
    get
    {
      return _title;
    }
    set
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        if (value == null !)
        {
          throw new ArgumentNullException("value", "The 'Title' property is required.");
        }
        else
        {
          throw new ArgumentException("The 'Title' property is required.", "value");
        }
      }
      _title = value;
    }
  }
}
