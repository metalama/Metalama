class TargetCode
{
  Action<string, dynamic> dynamicGeneric;
  dynamic[] dynamicArray;
  (dynamic, int) dynamicTuple;
  ref dynamic DynamicRef => throw new Exception();
  Action<string, Func<dynamic, object>> dynamicConstructionGeneric;
  Func<dynamic, object>[] dynamicConstructionArray;
  (Func<dynamic, object>, int) dynamicConstructionTuple;
  ref Func<dynamic, object> DynamicConstructionRef => throw new Exception();
}