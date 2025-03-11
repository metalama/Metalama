// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.PartialType_SyntaxTrees
{
    // <target>
    internal partial class TargetClass
    {
        public event EventHandler TargetEvent2
        {
            add => Console.WriteLine("This is TargetEvent2.");
            remove => Console.WriteLine("This is TargetEvent2.");
        }
    }
}