// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Diagnostics.CodeAnalysis;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0649

namespace Metalama.Framework.Tests.LinkerTests.Tests.Naming.BackingFieldConflict
{
    // <target>
    [SuppressMessage( "Style", "IDE1006:Naming Styles" )]
    internal class Target
    {
        private int _property;
        private int _property1;
        private int _property2;
        private int _property3;
        private int _property4;
        private int _property5;
        private int _property6;
        private int _property7;
        private int _property8;
        private int _property9;
        private int _property10;

        public int Property { get; set; }

        [PseudoOverride( nameof(Property), "TestAspect" )]
        public int Property_Override
        {
            get
            {
                return Link[This.Property.get];
            }
            set
            {
                Link[This.Property.set] = value;
            }
        }

        public int Property1 { get; set; }

        [PseudoOverride( nameof(Property1), "TestAspect" )]
        public int Property1_Override
        {
            get
            {
                return Link[This.Property1.get];
            }
            set
            {
                Link[This.Property1.set] = value;
            }
        }
    }
}
