using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Services;

namespace Contract
{
    [CompileTime]
    public interface IContract : IProjectService
    {
        string? GetDocumentationCommentId( IDeclaration declaration );
    }
}