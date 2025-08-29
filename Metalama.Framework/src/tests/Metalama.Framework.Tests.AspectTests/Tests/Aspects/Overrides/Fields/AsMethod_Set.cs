// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.AsMethod_Set
{
    public class OverrideAttribute : FieldAspect
    {
        public override void BuildAspect( IAspectBuilder<IField> builder )
        {
            builder.With( builder.Target.SetMethod! ).Override( nameof( SetterTemplate ) );
        }

        [Template]
        public void SetterTemplate( int value )
        {
            Console.WriteLine( "Overridden setter" );
            meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public int Field = 42;

        [Override]
        public static int StaticField = 24;
    }
}