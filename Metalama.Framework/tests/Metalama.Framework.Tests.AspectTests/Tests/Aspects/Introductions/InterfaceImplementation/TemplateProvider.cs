using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TemplateProvider
{
    /*
     * Tests a simple case with implicit interface member defined in a template provider.
     */

    public interface IInterface
    {
        void Foo();

        void Bar();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.Advice.WithTemplateProvider( new TemplateProvider() ).ImplementInterface( aspectBuilder.Target, typeof(IInterface) );
        }
    }

    public class TemplateProviderBase : ITemplateProvider
    {
        [InterfaceMember]
        public void Foo()
        {
            Console.WriteLine( "Introduced interface member" );
        }
    }

    public class TemplateProvider : TemplateProviderBase
    {
        [InterfaceMember]
        public void Bar()
        {
            Console.WriteLine( "Introduced interface member" );
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}