// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Delegates.IntoNamespace
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            // A top-level delegate takes a different route through the design-time generator than a nested one,
            // which is why it is tested separately.
            builder.With( builder.Target.ContainingNamespace )
                .IntroduceDelegate(
                    "TopLevelHandler",
                    buildDelegate: d =>
                    {
                        d.Accessibility = Accessibility.Public;
                        d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                        d.AddParameter( "value", typeof(string) );
                    } );
        }
    }

    // <target>
    namespace TargetNamespace
    {
        [Introduction]
        public class TargetType { }
    }
}
