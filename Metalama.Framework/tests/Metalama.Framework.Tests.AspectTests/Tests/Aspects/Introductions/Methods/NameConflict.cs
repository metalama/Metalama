using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.NameConflict
{
    /*
     * Verifies that names coming from the method builder are included in lexical scope of the property.
     */
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod(
                nameof(ParameterConflict),
                buildMethod: introduced =>
                {
                    introduced.Name = "Method_ParameterConflict";
                    introduced.AddParameter( "x", typeof(int) );
                } );

            builder.IntroduceMethod(
                nameof(NameConflict),
                buildMethod: introduced => { introduced.Name = "Method_NameConflict"; } );

            builder.IntroduceMethod(
                nameof(TypeParameterConflict),
                buildMethod: introduced =>
                {
                    introduced.Name = "Method_GenericParameterConflict";
                    introduced.AddTypeParameter( "TParameter" );
                } );
        }

        [Template]
        public dynamic? ParameterConflict()
        {
            meta.InsertComment( "Forces conflict between the method parameter name and name of the local variable." );
            var x = meta.Proceed();

            Console.WriteLine( "This is introduced method." );

            return x;
        }

        [Template]
        public int NameConflict( int p )
        {
            meta.InsertComment( "Forces conflict between the method name (which is different than template name) and a local function." );
            meta.InsertComment( "If the local function is not renamed, an error is produced due to different return type." );
            var x = meta.Proceed();

            Console.WriteLine( "This is introduced method." );

            if (p > 0)
            {
                return ExpressionFactory.Parse( "Method_NameConflict(p - 1)" ).Value;
            }

            Method_NameConflict();

            return p;

            void Method_NameConflict() { }
        }

        [Template]
        public dynamic? TypeParameterConflict()
        {
            meta.InsertComment( "Forces conflict between the method type parameter name and name of the local variable." );
            var TParameter = meta.Proceed();

            Console.WriteLine( "This is introduced method." );

            return TParameter;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}