namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.InheritableTypeFabric_CrossAssembly
{

     // <target>
    internal class DerivedClass : BaseClass
    {
        private int Method3( int a ) => a;

        private string Method4( string s ) => s;
    }
}