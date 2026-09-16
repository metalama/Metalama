[Introduction]
public class TargetType
{
  public enum Signed : global::System.SByte
  {
    Min = -128,
    Max = 127
  }
  public enum UnsignedByte : global::System.Byte
  {
    Min = 0,
    Max = 255
  }
  public enum UnsignedInt : global::System.UInt32
  {
    Max = 4294967295
  }
  public enum UnsignedLong : global::System.UInt64
  {
    Zero = 0,
    Max = 18446744073709551615
  }
  public enum UnsignedShort : global::System.UInt16
  {
    Max = 65535
  }
}