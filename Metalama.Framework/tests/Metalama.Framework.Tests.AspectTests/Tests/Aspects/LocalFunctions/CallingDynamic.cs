#if TEST_OPTIONS
// @FormatOutput
#endif
using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.CallingDynamic;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void M( int a )
    {
        var canExecuteMethod = meta.Target.Type.Methods.OfName( "CanExecute" ).Single();
        var executeMethod = meta.Target.Type.Methods.OfName( "Execute1" ).Single();

        var canExecuteExpression = ExpressionFactory.Capture(
            new Func<object, bool>( parameter => (bool)canExecuteMethod.Invoke( meta.Cast( meta.Target.Parameters[0].Type, parameter ) ) ) );

        var executeExpression = ExpressionFactory.Capture( new Action( () => { executeMethod.Invoke( new object() ); } ) );

        // These references should NOT be simplified.
        if (canExecuteExpression.Value.Invoke( meta.Target.Parameters[0].Value ))
        {
            Console.WriteLine( "Hello, world." );
        }

        C.Execute1( canExecuteExpression.Value );
        _ = new C( canExecuteExpression.Value );
        var x = canExecuteExpression.Value;
        _ = new C { UntypedProperty = executeExpression.Value };

        // The references below should be simplified.

        C.Execute2( canExecuteExpression.Value );
        _ = new C( null, canExecuteExpression.Value );
        var y = executeExpression.Value;
        _ = new C { TypedProperty = executeExpression.Value };
    }
}

// <target>
[TheAspect]
internal class C
{
    public C() { }

    public C( object o ) { }

    public C( object? o, Func<object, bool> f ) { }

    public bool CanExecute( object? x ) => x != null;

    public static void Execute1( object o ) { }

    public static void Execute2( Func<object, bool> f ) => f( new object() );

    public Action? TypedProperty { get; set; }

    public object? UntypedProperty { get; set; }
}