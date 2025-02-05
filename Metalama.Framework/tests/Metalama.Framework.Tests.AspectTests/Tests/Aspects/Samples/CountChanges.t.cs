[CountChanges]
internal class C
{
  private string? _address;
  public string? Address
  {
    get
    {
      return _address;
    }
    set
    {
      var oldValue = _address;
      _address = value;
      if (oldValue != _address)
      {
        AddressChangeCount = AddressChangeCount + 1;
      }
    }
  }
  private int _items;
  public int Items
  {
    get
    {
      return _items;
    }
    set
    {
      var oldValue = _items;
      _items = value;
      if (oldValue != _items)
      {
        ItemsChangeCount = ItemsChangeCount + 1;
      }
    }
  }
  public int AddressChangeCount { get; set; }
  public int ItemsChangeCount { get; set; }
  public int TotalChanges
  {
    get
    {
      var sum = 0;
      sum += AddressChangeCount;
      sum += ItemsChangeCount;
      return sum;
    }
  }
}