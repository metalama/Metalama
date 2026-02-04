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
  ~C()
  {
  }
  private static global::System.String[] GetAllBuiltIds()
  {
    return new global::System.String[]
    {
      "C.Event",
      "C._field",
      "C._field.get",
      "C._field.get@<return>",
      "C._field.set",
      "C._field.set@value",
      "C._field.set@<return>",
      "C.C(int)",
      "C.C(int)@x",
      "C.C(string)",
      "C.C(string)/id",
      "C.Event.add",
      "C.Event.add@value",
      "C.Event.add@<return>",
      "C.~C()",
      "C.~C()@<return>",
      "C.this[int].get",
      "C.this[int].get@index",
      "C.this[int].get@<return>",
      "C.Property.get",
      "C.Property.get@<return>",
      "C.M<T>((int x, int y))",
      "C.M<T>((int x, int y))@p",
      "C.M<T>((int x, int y))@<return>",
      "C.M<T>((int x, int y))/T",
      "C.operator +(C, C)",
      "C.operator +(C, C)@x",
      "C.operator +(C, C)@y",
      "C.operator +(C, C)@<return>",
      "C.explicit operator(C)",
      "C.explicit operator(C)@x",
      "C.explicit operator(C)@<return>",
      "C.operator !(C)",
      "C.operator !(C)@x",
      "C.operator !(C)@<return>",
      "C.Event.remove",
      "C.Event.remove@value",
      "C.Event.remove@<return>",
      "C.this[int].set",
      "C.this[int].set@index",
      "C.this[int].set@value",
      "C.this[int].set@<return>",
      "C.Property.set",
      "C.Property.set@value",
      "C.Property.set@<return>",
      "C.this[int]",
      "C.this[int]@index",
      "C.Property",
      "C"
    };
  }
  private void M<T>((global::System.Int32 x, global::System.Int32 y) p)
  {
  }
  public static global::System.Int32 operator +(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C x, global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C y)
  {
    return (global::System.Int32)0;
  }
  public static explicit operator global::System.Boolean(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C x)
  {
    return (global::System.Boolean)true;
  }
  public static global::System.Boolean operator !(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C x)
  {
    return (global::System.Boolean)false;
  }
  private event global::System.EventHandler? Event;
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
