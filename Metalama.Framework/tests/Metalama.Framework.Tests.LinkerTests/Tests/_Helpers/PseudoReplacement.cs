using System;

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    [AttributeUsage(AttributeTargets.All)]
    public class PseudoReplacement : Attribute
    {
        public PseudoReplacement(string targetMember)
        {
        }
    }
}
