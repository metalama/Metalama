internal class TargetClass
{
  public TargetClass(int x)
  {
  }
  public int Field;
  public int Property { get; set; }
  [Override]
  public void TargetMethod_Void(int x)
  {
    var methodInfo = ((global::System.Reflection.MethodInfo)(typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetMethod("TargetMethod_Void", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The method 'TargetClass.TargetMethod_Void(int)' could not be found using reflection.")));
    var methodBase = ((global::System.Reflection.MethodInfo)(typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetMethod("TargetMethod_Void", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The method 'TargetClass.TargetMethod_Void(int)' could not be found using reflection.")));
    var memberInfo = ((global::System.Reflection.MethodInfo)(typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetMethod("TargetMethod_Void", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The method 'TargetClass.TargetMethod_Void(int)' could not be found using reflection.")));
    var parameterInfo = (typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetMethod("TargetMethod_Void", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The method 'TargetClass.TargetMethod_Void(int)' could not be found using reflection.")).GetParameters()[0];
    var returnValueInfo = (typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetMethod("TargetMethod_Void", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The method 'TargetClass.TargetMethod_Void(int)' could not be found using reflection.")).ReturnParameter;
    var type = typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass);
    var field = new global::Metalama.Framework.RunTime.FieldOrPropertyInfo((typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetField("Field", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance) ?? throw new global::System.MissingFieldException("The field 'TargetClass.Field' could not be found using reflection.")));
    var propertyAsFieldOrProperty = new global::Metalama.Framework.RunTime.FieldOrPropertyInfo((typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetProperty("Property", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance) ?? throw new global::System.MissingMemberException("The property 'TargetClass.Property' could not be found using reflection.")));
    var property = (typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetProperty("Property", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance) ?? throw new global::System.MissingMemberException("The property 'TargetClass.Property' could not be found using reflection."));
    var constructor = ((global::System.Reflection.ConstructorInfo)(typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The constructor 'TargetClass.TargetClass(int)' could not be found using reflection.")));
    var constructorParameter = (typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The constructor 'TargetClass.TargetClass(int)' could not be found using reflection.")).GetParameters()[0];
    var array = new global::System.Reflection.MemberInfo[]
    {
      ((global::System.Reflection.MethodInfo)(typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass).GetMethod("TargetMethod_Void", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null) ?? throw new global::System.MissingMethodException("The method 'TargetClass.TargetMethod_Void(int)' could not be found using reflection."))),
      typeof(global::Metalama.Framework.IntegrationTests.Aspects.CodeModel.SyntaxSerializer.TargetClass)
    };
    return;
  }
  public int TargetMethod_Int(int x)
  {
    return 0;
  }
}