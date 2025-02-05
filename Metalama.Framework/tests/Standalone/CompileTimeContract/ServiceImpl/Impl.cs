using Contract;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;

namespace ServiceImpl
{
    [MetalamaPlugIn]
    public class Impl : IContract
    {
        public string? GetDocumentationCommentId( IDeclaration declaration )
            => declaration.GetSymbol().GetDocumentationCommentId();
    }
}