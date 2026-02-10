// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.ComponentModel;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias
{
    /*
     * Tests that trivia (doc comments) are correctly handled when overriding fields.
     * When a field is overridden, the field becomes a backing field and a property is generated.
     * The doc comment should stay on the public member (the property), not the backing field.
     */

    public class OverrideAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );
                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        /// <summary>
        /// A simple field.
        /// </summary>
        [Override]
        public int Field;

        // Regular comment before field.
        /// <summary>
        /// A field with a regular comment.
        /// </summary>
        [Override]
        [Description( "A described field" )]
        public int FieldWithComment;

        /// <summary>
        /// A static field.
        /// </summary>
        [Override]
        public static int StaticField;
    }
}
