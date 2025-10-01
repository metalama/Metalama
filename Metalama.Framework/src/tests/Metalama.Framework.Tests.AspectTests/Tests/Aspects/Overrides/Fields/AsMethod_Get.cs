// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Get
{
    public class OverrideAttribute : FieldAspect
    {
        public override void BuildAspect( IAspectBuilder<IField> builder )
        {
            builder.With( builder.Target.GetMethod! ).Override( nameof( GetterTemplate ) );
        }

        [Template]
        public dynamic? GetterTemplate()
        {
            Console.WriteLine( "Overridden getter" );
            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public int Field = 42;

        [Override]
        public static int StaticField = 24;

        [Override]
        public readonly int ReadOnlyField = 12;
    }
}