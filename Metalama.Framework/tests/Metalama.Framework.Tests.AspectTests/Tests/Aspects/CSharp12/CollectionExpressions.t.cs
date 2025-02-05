[TheAspect]
private static void M(int i)
{
  int[] collection1 = [1, 2, 3, ..global::System.Linq.Enumerable.Range(3, 2)];
  global::System.Console.WriteLine(collection1);
  int[] collection2 = [i, 2, 3, ..global::System.Linq.Enumerable.Range(3, 2)];
  global::System.Console.WriteLine(collection2);
  global::System.Console.WriteLine(new global::System.Int32[] { 1, 2, 3, 3, 4 });
  global::System.Console.WriteLine(new global::System.Int32[] { 1, 2, 3, 3, 4 });
  global::System.Console.WriteLine(new global::System.Int32[] { 1, 2, 3, 3, 4 });
  int[] collection = [1, 2, ..Enumerable.Range(3, 2)];
  return;
}