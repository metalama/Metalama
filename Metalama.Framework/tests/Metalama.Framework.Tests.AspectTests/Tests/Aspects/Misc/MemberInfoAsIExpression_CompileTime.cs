using System;
using System.Reflection;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

#if TEST_OPTIONS
// @MainMethod(TestMain)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_CompileTime;

#pragma warning disable CS0618 // Select is obsolete

public sealed class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var arrayBuilder = new ArrayBuilder( typeof(object) );

        var types = new Type[] { typeof(RunTimeClass), typeof(RunTimeOrCompileTimeClass) };

        foreach (var type in types)
        {
            var members = ( (INamedType)TypeFactory.GetType( type ) ).Members();

            if (type.Name == nameof(RunTimeClass))
            {
                arrayBuilder.Add( type.ToExpression() );
            }

            foreach (var member in members)
            {
                arrayBuilder.Add( member.ToMemberInfo().ToExpression() );

                if (member is IField field)
                {
                    arrayBuilder.Add( field.ToFieldInfo().ToExpression() );
                }

                if (member is IProperty property)
                {
                    arrayBuilder.Add( property.ToPropertyInfo().ToExpression() );
                }

                if (member is IMethod method)
                {
                    arrayBuilder.Add( method.ReturnParameter.ToParameterInfo().ToExpression() );
                }

                if (member is IHasParameters hasParameters)
                {
                    foreach (var parameter in hasParameters.Parameters)
                    {
                        arrayBuilder.Add( parameter.ToParameterInfo().ToExpression() );
                    }
                }
            }
        }

        builder.IntroduceField(
            "members",
            typeof(object[]),
            buildField: b => { b.InitializerExpression = arrayBuilder.ToExpression(); } );
    }
}

internal class Program
{
    private static void TestMain() => new TargetCode();
}

// <target>
[TestAspect]
internal class TargetCode
{
    public TargetCode()
    {
        var members = (object[])GetType().GetField( "members", BindingFlags.Instance | BindingFlags.NonPublic )!.GetValue( this )!;

        for (var i = 0; i < members.Length; i++)
        {
            if (members[i] == null)
            {
                throw new Exception( $"Member at index {i} was not resolved correctly." );
            }
        }
    }
}

internal class RunTimeClass
{
    private void M( int i ) { }

    private int P { get; set; }

    private event EventHandler E
    {
        add { }
        remove { }
    }

    private int this[ int i ]
    {
        get => 42;
        set { }
    }
}

[RunTimeOrCompileTime]
internal class RunTimeOrCompileTimeClass
{
    private void M( int i ) { }

    private int P { get; set; }

    private event EventHandler E
    {
        add { }
        remove { }
    }

    // indexers are not supported in compile-time code
}