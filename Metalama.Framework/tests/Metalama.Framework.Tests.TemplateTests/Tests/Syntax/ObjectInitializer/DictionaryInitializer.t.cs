private int Method(int a, int b)
{
  var x = new global::System.Collections.Generic.Dictionary<global::System.Int32, global::System.Int32>
  {
    [1] = 1,
    [2] = 2,
    [3] = 3
  };
  global::System.Console.WriteLine("[1, 2], [2, 2], [3, 3]");
  var z = new global::System.Collections.Generic.Dictionary<global::System.Int32, global::System.Int32>
  {
    [1] = a,
    [2] = 2,
    [3] = 3
  };
  global::System.Collections.Generic.Dictionary<global::System.String, global::System.String> report = new()
  {
    {
      "Title",
      "Method"
    },
    {
      "ID",
      global::System.Guid.NewGuid().ToString()
    },
    {
      "HTTP result",
      "400"
    },
    {
      "Exception type",
      a.ToString()
    }
  };
  return default;
}