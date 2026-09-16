[Introduction]
[Union]
public class Pet
{
  public Pet(int cat)
  {
    this.Value = cat;
  }
  public object? Value { get; }
  private global::System.Int32 _field;
  private global::System.Int32 Property { get; set; }
  public event global::System.EventHandler? FieldLikeEvent;
}