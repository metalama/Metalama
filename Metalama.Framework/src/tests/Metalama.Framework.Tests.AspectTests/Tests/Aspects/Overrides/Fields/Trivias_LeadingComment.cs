// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Trivias_LeadingComment
{
    /*
     * Tests that when a field is promoted to a property by an aspect:
     * - Regular comments (// and block comments) stay with the backing field (not moved to the property).
     * - XML documentation comments (///) are transferred to the property (public semantic).
     * - When both regular and doc comments are present, they are split correctly.
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
        // Applied on a field.
        [Override]
        public string LastName;

        // First comment.
        // Second comment.
        [Override]
        public int MultipleComments;

        /* Block comment before field. */
        [Override]
        public int BlockCommentField;

        /// <summary>
        /// XML doc comment on field.
        /// </summary>
        [Override]
        public int DocCommentField;

        // Regular comment before doc.
        /// <summary>
        /// Mixed: regular + doc comment.
        /// </summary>
        [Override]
        public int MixedComments;
    }
}
