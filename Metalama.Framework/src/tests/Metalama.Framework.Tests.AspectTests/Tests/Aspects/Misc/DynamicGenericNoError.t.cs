internal class TargetCode
{
  private Action<string, dynamic> dynamicGeneric;
  private dynamic[] dynamicArray;
  private (dynamic, int) dynamicTuple;
  private ref dynamic DynamicRef => throw new Exception();
  private Action<string, Func<dynamic, object>> dynamicConstructionGeneric;
  private Func<dynamic, object>[] dynamicConstructionArray;
  private (Func<dynamic, object>, int) dynamicConstructionTuple;
  private ref Func<dynamic, object> DynamicConstructionRef => throw new Exception();
}