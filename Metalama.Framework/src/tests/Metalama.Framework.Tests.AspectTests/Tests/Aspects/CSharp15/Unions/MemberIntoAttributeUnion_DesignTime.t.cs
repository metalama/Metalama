[Introduction]
[Union]
public partial struct Pet
{
  public Pet(int cat)
  {
    this.Value = cat;
  }
  public object? Value { get; }
}