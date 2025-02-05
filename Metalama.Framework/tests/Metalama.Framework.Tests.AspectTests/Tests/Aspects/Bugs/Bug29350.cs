using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.Bug29350
{
    public class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            // Build a formatting string.
            var methodName = BuildInterpolatedString();

            // Write entry message.
            var entryMessage = methodName.Clone();
            entryMessage.AddText( " started." );
            Console.WriteLine( entryMessage.ToValue() );

            try
            {
                // Invoke the method.
                var result = meta.Proceed();

                // Display the success message.
                var successMessage = methodName.Clone();

                if (meta.Target.Method.ReturnType.IsConvertibleTo( typeof(void) ))
                {
                    successMessage.AddText( " succeeded." );
                }
                else
                {
                    successMessage.AddText( " returned " );
                    successMessage.AddExpression( result );
                    successMessage.AddText( "." );
                }

                Console.WriteLine( successMessage.ToValue() );

                return result;
            }
            catch (Exception e)
            {
                // Display the failure message.
                var failureMessage = methodName.Clone();
                failureMessage.AddText( " failed: " );
                failureMessage.AddExpression( e.Message );
                Console.WriteLine( failureMessage.ToValue() );

                throw;
            }
        }

        private static InterpolatedStringBuilder BuildInterpolatedString()
        {
            var stringBuilder = new InterpolatedStringBuilder();
            stringBuilder.AddText( meta.Target.Type.ToDisplayString( CodeDisplayFormat.MinimallyQualified ) );
            stringBuilder.AddText( "." );
            stringBuilder.AddText( meta.Target.Method.Name );
            stringBuilder.AddText( "(" );
            var i = 0;

            foreach (var p in meta.Target.Parameters)
            {
                var comma = i > 0 ? ", " : "";

                if (p.RefKind == RefKind.Out)
                {
                    stringBuilder.AddText( $"{comma}{p.Name} = <out> " );
                }
                else
                {
                    stringBuilder.AddText( $"{comma}{p.Name} = {{" );
                    stringBuilder.AddExpression( p );
                    stringBuilder.AddText( "}" );
                }

                i++;
            }

            stringBuilder.AddText( ")" );

            return stringBuilder;
        }
    }

    // <target>
    internal class Program
    {
        private static void TestMain()
        {
            Add( 1, 1 );
        }

        [Log]
        private static int Add( int a, int b )
        {
            return a + b;
        }
    }
}