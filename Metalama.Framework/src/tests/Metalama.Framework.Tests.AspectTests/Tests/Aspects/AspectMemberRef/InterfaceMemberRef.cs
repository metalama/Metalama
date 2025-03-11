// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.InterfaceMemberRef
{
    public class IntroduceAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.ImplementInterface( typeof(IInterface) );
        }

        [Framework.Aspects.Introduce]
        public void SomeMethod()
        {
            Method();
            Property = Property + 1;
            Event += EventHandler;
        }

        [Framework.Aspects.Introduce]
        private void EventHandler( object? sender, EventArgs a ) { }

        [InterfaceMember]
        public void Method() { }

        [InterfaceMember]
        public int Property { get; set; }

        [InterfaceMember]
        public event EventHandler? Event;
    }

    internal interface IInterface
    {
        void Method();

        int Property { get; set; }

        event EventHandler? Event;
    }

    // <target>
    [Introduce]
    internal class Program { }
}