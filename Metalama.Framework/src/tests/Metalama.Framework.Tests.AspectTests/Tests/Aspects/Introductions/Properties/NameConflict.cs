// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.NameConflict
{
    /*
     * Verifies that names coming from the property builder are included in lexical scope of the property.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty(
                "Property_NameConflict",
                nameof(NameConflict_Get),
                nameof(NameConflict_Set) );

            builder.IntroduceProperty(
                "Property_ValueConflict",
                nameof(ValueConflict_Get),
                nameof(ValueConflict_Set) );
        }

        [Template]
        public int NameConflict_Get()
        {
            var Property_NameConflict = $"{ExpressionFactory.Parse( "Property_NameConflict" ).Value}";

            return ExpressionFactory.Parse( "Property_NameConflict" ).Value + Property_NameConflict.Length;
        }

        [Template]
        public void NameConflict_Set( int value )
        {
            var Property_NameConflict = $"{ExpressionFactory.Parse( "Property_NameConflict" ).Value}";
            ExpressionFactory.Parse( "Property_NameConflict" ).Value = value + Property_NameConflict.Length;
        }

        [Template]
        public int ValueConflict_Get()
        {
            return 42;
        }

        [Template]
        public void ValueConflict_Set()
        {
            string value = ExpressionFactory.Parse( "value" ).Value!.ToString();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}