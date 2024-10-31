using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.CompileTime;

internal interface IReferenceAssemblyLocatorProvider : IGlobalService
{
    ReferenceAssemblyLocator GetInstance( in ProjectServiceProvider serviceProvider );
}