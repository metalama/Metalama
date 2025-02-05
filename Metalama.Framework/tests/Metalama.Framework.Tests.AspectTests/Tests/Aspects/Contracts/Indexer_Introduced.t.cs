[IntroduceAndFilter]
internal class Target
{
  public string? this[string? x, string? y]
  {
    get
    {
      if (x == null)
      {
        throw new global::System.ArgumentNullException();
      }
      if (y == null)
      {
        throw new global::System.ArgumentNullException();
      }
      global::System.String? returnValue;
      returnValue = x + y;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
    set
    {
      if (x == null)
      {
        throw new global::System.ArgumentNullException();
      }
      if (y == null)
      {
        throw new global::System.ArgumentNullException();
      }
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
    }
  }
  public global::System.String? this[global::System.String? index]
  {
    get
    {
      if (index == null)
      {
        throw new global::System.ArgumentNullException();
      }
      global::System.String? returnValue;
      returnValue = default(global::System.String? );
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
    set
    {
      if (index == null)
      {
        throw new global::System.ArgumentNullException();
      }
      if (value == null)
      {
        throw new global::System.ArgumentNullException();
      }
    }
  }
}