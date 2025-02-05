internal class TargetCode<T>
{
  [Aspect]
  private Task<T> GenericClassTypeParameter()
  {
    global::System.Console.WriteLine(typeof(global::System.Threading.Tasks.Task<T>));
    return null !;
  }
  [Aspect]
  private Task<TM> GenericMethodTypeParameter<TM>()
  {
    global::System.Console.WriteLine(typeof(global::System.Threading.Tasks.Task<TM>));
    return null !;
  }
  [Aspect]
  private Task<int> ClosedGeneric()
  {
    global::System.Console.WriteLine(typeof(global::System.Threading.Tasks.Task<global::System.Int32>));
    return null !;
  }
  [Aspect]
  private T[] ArrayClassTypeParameter()
  {
    global::System.Console.WriteLine(typeof(T[]));
    return null !;
  }
  [Aspect]
  private TM[] ArrayMethodTypeParameter<TM>()
  {
    global::System.Console.WriteLine(typeof(TM[]));
    return null !;
  }
  [Aspect]
  private int[] ClosedArray()
  {
    global::System.Console.WriteLine(typeof(global::System.Int32[]));
    return null !;
  }
  [Aspect]
  private Action<T, TM, T[], TM[], int, int[], Action<T, TM>> ComplexType<TM>()
  {
    global::System.Console.WriteLine(typeof(global::System.Action<T, TM, T[], TM[], global::System.Int32, global::System.Int32[], global::System.Action<T, TM>>));
    return null !;
  }
}