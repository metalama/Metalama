// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Linq;

#pragma warning disable CS0414

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.InitializerTemplate_CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler? IntroducedEventField = meta.Target.Event.DeclaringType.Fields.OfName( "Foo" ).Single().Value;

        [Introduce]
        public static event EventHandler? IntroducedEventField_Static = meta.Target.Event.DeclaringType.Fields.OfName( "Foo" ).Single().Value;
    }
}