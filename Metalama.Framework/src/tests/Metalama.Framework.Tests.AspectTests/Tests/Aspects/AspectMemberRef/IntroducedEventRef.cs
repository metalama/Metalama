// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.IntroducedEventRef
{
    public class RetryAttribute : TypeAspect
    {
        [Introduce]
        private void IntroducedMethod1( string name )
        {
            MyEvent?.Invoke( meta.This, new PropertyChangedEventArgs( name ) );
            MyEvent( meta.This, new PropertyChangedEventArgs( name ) );
        }

        [Introduce]
        private event PropertyChangedEventHandler? MyEvent;
    }

    // <target>
    [Retry]
    internal class Program { }
}