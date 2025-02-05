using System;

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class PseudoLayerOrder : Attribute
    {
        public PseudoLayerOrder(string aspectName, string? layerName = null)
        {
        }
    }
}
