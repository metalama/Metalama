using System;

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    [AttributeUsage(AttributeTargets.All)]
    public class PseudoIntroduction : Attribute
    {
        public PseudoIntroduction(string aspectName, string? layerName = null)
        {
        }
    }
}
