[OverrideEqualityContractAttribute]
internal record Target
{
  protected virtual global::System.Type EqualityContract
  {
    get
    {
      // <target>
      global::System.Console.WriteLine("Aspect code.");
      throw new global::System.NotSupportedException("Calling the original implementation of a compiler-synthesized record member is not supported. Do not use meta.Proceed() when overriding synthesized record members like Equals or GetHashCode.");
    }
  }
}