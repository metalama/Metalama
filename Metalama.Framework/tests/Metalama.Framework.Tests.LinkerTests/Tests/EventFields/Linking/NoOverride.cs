using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Linking.NoOverride
{
    // <target>
    class Target
    {
        [PseudoIntroduction("TestAspect")]
        [PseudoNotInlineable]
        public event EventHandler? Bar;
    }
}
