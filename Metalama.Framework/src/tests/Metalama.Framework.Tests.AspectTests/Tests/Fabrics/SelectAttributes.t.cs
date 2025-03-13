[Marker]
public class C
{
  public void UnmarkedMethod()
  {
  }
  [Marker(Value = "TheMarker")]
  public void MarkedMethod()
  {
    global::System.Console.WriteLine("Marker: TheMarker");
    return;
  }
  [DerivedMarker(Value = "DerivedMarker")]
  public void DerivedMethod()
  {
    global::System.Console.WriteLine("Marker: DerivedMarker");
    return;
  }
}