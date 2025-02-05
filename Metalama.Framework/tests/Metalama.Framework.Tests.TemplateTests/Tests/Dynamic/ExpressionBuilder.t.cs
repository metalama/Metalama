private int Method(int a, string c, DateTime dt)
{
  global::System.Console.WriteLine("type=object");
  var value = Test((sbyte)1, 1D, 1F, "s\"\n", 1M, 1L, 1UL, (byte)1, (sbyte)1, (short)1, (ushort)1, 42, new global::System.Guid(80668217, -21262, 18147, 190, 62, 145, 96, 137, 199, 42, 30), new global::System.Object[] { 1, 2, 3, new global::System.Guid(80668217, -21262, 18147, 190, 62, 145, 96, 137, 199, 42, 30) }, a, typeof(global::System.Int32));
  return default;
}