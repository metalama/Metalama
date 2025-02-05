// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;

namespace Metalama.Framework.DesignTime.Pipeline;

public sealed class PipelineResultBasedAspectRepository : AspectRepository
{
    private readonly DesignTimeAspectPipelineResult _result;

    public PipelineResultBasedAspectRepository( DesignTimeAspectPipelineResult result )
    {
        this._result = result;
    }

    public override AspectRepository WithAspectInstances( IEnumerable<IAspectInstance> aspectInstances, CompilationModel compilation )
        => throw new NotSupportedException();

    public override bool HasAspect( IDeclaration declaration, Type aspectType )
    {
        var aspectFullName = aspectType.FullName;
        var syntaxTree = declaration.GetPrimarySyntaxTree();

        if ( syntaxTree == null )
        {
            return false;
        }

        if ( !this._result.SyntaxTreeResults.TryGetValue( syntaxTree.FilePath, out var syntaxTreeResult ) )
        {
            return false;
        }

        var declarationId = declaration.ToSerializableId();

        var aspectClass = this._result.Configuration?.AspectClasses.OfType<AspectClass>()
            .FirstOrDefault( aspectClass => aspectClass.FullName == aspectFullName );

        var descentantClassesNames = new HashSet<string>( aspectClass?.DescendantClassesAndSelf.Select( c => c.FullName ) ?? [] );

        return syntaxTreeResult.AspectInstances.Any(
            a => (a.AspectClassFullName == aspectFullName || descentantClassesNames.Contains( a.AspectClassFullName ))
                 && a.TargetDeclarationId == declarationId );
    }

    public override IEnumerable<IAspectInstance> GetAspectInstances( IDeclaration declaration )
    {
        throw new NotSupportedException( $"The GetAspects method is not supported to evaluate eligibility. Use HasAspect." );
    }
}