// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

#pragma warning disable CS0414

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.InitializerTemplate_CrossAssembly
{
    // <target>
    [Introduction]
    internal class TargetClass 
    {
        public static EventHandler Foo = new EventHandler(Bar);

        public static void Bar(object? sender, EventArgs eventArgs)
        {
        }
    }
}