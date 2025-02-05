using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

Console.WriteLine();

class Fabric : ProjectFabric
{
    public override void AmendProject(IProjectAmender amender)
    {
        amender.Outbound.SelectMany(compilation => compilation.Types).Where(t => t.IsNew).AddAspect<Aspect>();
    }
}

class Aspect : TypeAspect {}