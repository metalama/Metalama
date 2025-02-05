using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.TemplateClassMembers
{
    internal class Aspect : BaseAspect
    {
        public Aspect() : this( "Result = {0}" ) { }

        public Aspect( string formatString ) : base( "Result = {0}" ) { }

        [TestTemplate]
        private dynamic? Template()
        {
            var result = meta.Proceed();

            Console.WriteLine( this.Format( result ) );

            return result;
        }

        public override string? Format( object? o )
        {
            return o == null ? null : string.Format( FormatString, o );
        }
    }

    [CompileTime]
    internal abstract class BaseAspect
    {
        protected BaseAspect( string formatString )
        {
            FormatString = formatString;
        }

        public string FormatString { get; set; }

        public abstract string? Format( object? o );
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}