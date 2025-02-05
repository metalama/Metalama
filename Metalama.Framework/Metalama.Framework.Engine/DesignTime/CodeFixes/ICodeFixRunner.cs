// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.DesignTime.CodeFixes;

public interface ICodeFixRunner : IProjectService
{
    Task<CodeActionResult> ExecuteCodeFixAsync(
        Compilation compilation,
        SyntaxTree syntaxTree,
        string diagnosticId,
        TextSpan diagnosticSpan,
        string codeFixTitle,
        TestableCancellationToken cancellationToken );
}

public interface IStandaloneCodeFixRunnerFactory
{
    public ICodeFixRunner CreateCodeFixRunner( ProjectServiceProvider serviceProvider, CompileTimeDomain domain );
}