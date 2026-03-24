[TestAspect]
internal class TargetCode
{
  private int _field;
  private void M(int i)
  {
  }
  private int P { get; set; }
  private event EventHandler E
  {
    add
    {
    }
    remove
    {
    }
  }
  public void Run()
  {
    var methodInfo = ((global::System.Reflection.MethodInfo)(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.ToReflectionNullCheck.TargetCode).GetMethod("M", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The method 'TargetCode.M(int)' could not be found using reflection.")));
    var fieldInfo = typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.ToReflectionNullCheck.TargetCode).GetField("_field", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance) ?? throw new global::System.MissingFieldException("The field 'TargetCode._field' could not be found using reflection.");
    var propertyInfo = typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.ToReflectionNullCheck.TargetCode).GetProperty("P", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance) ?? throw new global::System.MissingMemberException("The property 'TargetCode.P' could not be found using reflection.");
    var eventInfo = typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.ToReflectionNullCheck.TargetCode).GetEvent("E", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance) ?? throw new global::System.MissingMemberException("The event 'TargetCode.E' could not be found using reflection.");
    var constructorInfo = ((global::System.Reflection.ConstructorInfo)(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.CodeModel.ToReflectionNullCheck.TargetCode).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, global::System.Type.EmptyTypes, null) ?? throw new global::System.MissingMethodException("The constructor 'TargetCode.TargetCode()' could not be found using reflection.")));
    global::System.Console.WriteLine(methodInfo.Name);
    global::System.Console.WriteLine(fieldInfo.Name);
    global::System.Console.WriteLine(propertyInfo.Name);
    global::System.Console.WriteLine(eventInfo.Name);
    global::System.Console.WriteLine(constructorInfo.Name);
  }
}
