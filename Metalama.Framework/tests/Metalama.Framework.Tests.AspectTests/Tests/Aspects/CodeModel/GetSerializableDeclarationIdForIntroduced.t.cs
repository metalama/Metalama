[IntroduceMembers]
[Serialize]
internal class C
{
  private C(global::System.Int32 x = 42)
  {
  }
  private C(string id) : this()
  {
  }
  private global::System.Int32 _field;
  private global::System.Int32 Property { get; set; }
  private static global::System.String[] GetAllBuiltIds()
  {
    return (global::System.String[])new global::System.String[]
    {
      "E:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Event",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field;PropertyGet",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field;PropertyGetReturnParameter",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field;PropertySet",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field;PropertySetParameter",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field;PropertySetReturnParameter",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.#ctor(System.Int32)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.#ctor(System.Int32);Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.#ctor(System.String)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.#ctor(System.String);Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.add_Event(System.EventHandler)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.add_Event(System.EventHandler);Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.add_Event(System.EventHandler);Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Finalize",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Finalize;Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.get_Item(System.Int32)~System.Int32",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.get_Item(System.Int32)~System.Int32;Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.get_Item(System.Int32)~System.Int32;Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.get_Property~System.Int32",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.get_Property~System.Int32;Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.GetBuilderIds~System.String[]",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.GetBuilderIds~System.String[];Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.GetBuiltIds~System.String[]",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.GetBuiltIds~System.String[];Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32})",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32});Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32});Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32});TypeParameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Int32",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Int32;Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Int32;Parameter=1",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Int32;Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean;Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean;Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean;Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean;Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.remove_Event(System.EventHandler)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.remove_Event(System.EventHandler);Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.remove_Event(System.EventHandler);Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32);Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32);Parameter=1",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32);Return",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.set_Property(System.Int32)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.set_Property(System.Int32);Parameter=0",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.set_Property(System.Int32);Return",
      "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Item(System.Int32)",
      "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Item(System.Int32);Parameter=0",
      "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Property"
    };
  }
  private static global::System.String[] GetBuilderIds()
  {
    return (global::System.String[])new global::System.String[]
    {
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32})",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field",
      "E:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Event",
      "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Property",
      "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Item(System.Int32)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Int32",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.#ctor(System.Int32);Parameter=0"
    };
  }
  private static global::System.String[] GetBuiltIds()
  {
    return (global::System.String[])new global::System.String[]
    {
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32})",
      "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C._field",
      "E:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Event",
      "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Property",
      "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Item(System.Int32)",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Int32",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C)~System.Boolean",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.Finalize",
      "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C.#ctor(System.Int32);Parameter=0"
    };
  }
  private void M<T>((global::System.Int32 x, global::System.Int32 y) p)
  {
  }
  private event global::System.EventHandler? Event;
  ~C()
  {
  }
  public static global::System.Int32 operator +(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C x, global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C y)
  {
    return (global::System.Int32)0;
  }
  public static explicit operator global::System.Boolean(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C x)
  {
    return (global::System.Boolean)true;
  }
  public static global::System.Boolean operator !(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.GetSerializableDeclarationIdForIntroduced.C x)
  {
    return (global::System.Boolean)false;
  }
  private global::System.Int32 this[global::System.Int32 index]
  {
    get
    {
      return (global::System.Int32)0;
    }
    set
    {
    }
  }
}