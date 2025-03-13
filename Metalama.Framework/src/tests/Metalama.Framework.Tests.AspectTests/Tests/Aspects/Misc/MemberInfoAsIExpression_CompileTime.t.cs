[TestAspect]
internal class TargetCode
{
  public TargetCode()
  {
    var members = (object[])GetType().GetField("members", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
    for (var i = 0; i < members.Length; i++)
    {
      if (members[i] == null)
      {
        throw new Exception($"Member at index {i} was not resolved correctly.");
      }
    }
  }
  private global::System.Object[] members = new global::System.Object[]
  {
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass),
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetMethod("M", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetMethod("M", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null)!.ReturnParameter,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetMethod("M", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null)!.GetParameters()[0],
    new global::Metalama.Framework.RunTime.FieldOrPropertyInfo(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetProperty("P", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!),
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetProperty("P", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!,
    new global::Metalama.Framework.RunTime.FieldOrPropertyInfo(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetField("<P>k__BackingField", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!),
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetField("<P>k__BackingField", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetEvent("E", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetProperty("Item", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, typeof(global::System.Int32), new global::System.Type[] { typeof(global::System.Int32) }, null)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetProperty("Item", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, typeof(global::System.Int32), new global::System.Type[] { typeof(global::System.Int32) }, null)!.GetIndexParameters()[0],
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeClass).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, global::System.Type.EmptyTypes, null)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetMethod("M", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetMethod("M", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null)!.ReturnParameter,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetMethod("M", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance, null, new[] { typeof(global::System.Int32) }, null)!.GetParameters()[0],
    new global::Metalama.Framework.RunTime.FieldOrPropertyInfo(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetProperty("P", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!),
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetProperty("P", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!,
    new global::Metalama.Framework.RunTime.FieldOrPropertyInfo(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetField("<P>k__BackingField", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!),
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetField("<P>k__BackingField", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetEvent("E", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!,
    typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime.RunTimeOrCompileTimeClass).GetConstructor(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance, null, global::System.Type.EmptyTypes, null)!
  };
}