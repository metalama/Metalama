using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Template_PropertyExpressions
{
    /*
     * Tests that expression bodied template works against all kinds of bodies of the property.
     */

    // TODO: Get-only auto-property does not get override.

    internal class TestAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => ExpressionFactory.Parse( "default" );
            set => Console.WriteLine( "Overridden" );
        }
    }

    // <target>
    public class Target
    {
        [Test]
        public int BlockBodiedAccessors
        {
            get
            {
                Console.WriteLine( "Original" );

                return 42;
            }
            set
            {
                Console.WriteLine( "Original" );
            }
        }

        [Test]
        public int ExpressionBodiedAccessors
        {
            get => 42;
            set => Console.WriteLine( "Original" );
        }

        [Test]
        public int ExpressionBodiedProperty => 42;

        [Test]
        public int AutoProperty { get; set; }

        [Test]
        public int AutoGetOnlyProperty { get; }
    }
}