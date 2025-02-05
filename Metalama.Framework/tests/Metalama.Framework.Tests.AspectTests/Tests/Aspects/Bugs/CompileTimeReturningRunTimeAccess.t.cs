[Aspect]
[EntityField]
public string? Name
{
  get
  {
    var field = (global::System.Linq.Enumerable.SingleOrDefault(new global::Metalama.Framework.RunTime.FieldOrPropertyInfo(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeReturningRunTimeAccess.Target).GetProperty("Name", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance)!).GetCustomAttributes(true), x => x is global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeReturningRunTimeAccess.IEntityField) as global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeReturningRunTimeAccess.IEntityField) ?? throw new global::System.Exception("Unable to retrieve field info.");
    global::System.Console.WriteLine(field);
    return this._name;
  }
  set
  {
    this._name = value;
  }
}