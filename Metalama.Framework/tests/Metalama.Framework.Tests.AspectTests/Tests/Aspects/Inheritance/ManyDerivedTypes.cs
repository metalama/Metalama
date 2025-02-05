using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.ManyDerivedTypes
{
    [Inheritable]
    internal class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var m in builder.Target.Methods)
            {
                builder.With( m ).Override( nameof(Template) );
            }
        }

        [Template]
        private dynamic? Template()
        {
            Console.WriteLine( "Overridden!" );

            return meta.Proceed();
        }
    }

    [Aspect]
    public interface IBase { }

    public interface ISub1 : IBase { }

    public interface ISub2 : IBase { }

    public interface IDerived : ISub1, ISub2 { }

    public class BaseClass : IDerived
    {
        private void M() { }
    }

    public class DerivedClass : BaseClass
    {
        private void N() { }
    }
}