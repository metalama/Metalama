using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Redirection_Property;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(NotNullAttribute) )]

#pragma warning disable CS0169, CS0649

// Tests redirection of contracts from a property to a method parameter (used by DependencyProperty in Patterns).
// This does not test the multi-layered nature of the ContractAspect.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Redirection_Property
{
    internal class NotNullAttribute : ContractAspect
    {
        public override void Validate( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
        }
    }

    internal class RedirectingAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.IntroduceMethod( nameof(Foo) );

            ContractAspect.RedirectContracts( builder, builder.Target.Properties.OfName( "P" ).Single(), result.Declaration.Parameters[0] );
            ContractAspect.RedirectContracts( builder, builder.Target.Properties.OfName( "Q" ).Single(), result.Declaration.Parameters[1] );
        }

        [Template]
        public void Foo( string p, string q ) { }
    }

    // <target>
    [RedirectingAspect]
    internal class Target
    {
        private string? q;

        [NotNull]
        public string P => "p";

        [NotNull]
        public string Q
        {
            get
            {
                return q!;
            }
        }
    }
}