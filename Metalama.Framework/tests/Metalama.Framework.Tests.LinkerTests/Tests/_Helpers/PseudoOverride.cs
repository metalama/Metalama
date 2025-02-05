using System;

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    [AttributeUsage(AttributeTargets.All)]
    public class PseudoOverride : Attribute
    {
        public PseudoOverride(string targetMember, string aspectName, string? layerName= null)
        {
        }
    }
}
