internal class B
{
  public virtual string P { get; set; }
}
internal class C : B
{
  [NotNull]
  public override string P
  {
    get
    {
      global::System.String returnValue;
      returnValue = "C";
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
  }
}
internal class C2 : C
{
  public override string P
  {
    set
    {
    }
  }
}