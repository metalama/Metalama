// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

/// <summary>
/// Transformation that applies a contract to the receiver parameter of an extension block.
/// The contract is applied to all instance members (methods, property accessors, indexer accessors).
/// </summary>
internal sealed class ContractExtensionBlockTransformation : ContractBaseTransformation
{
    private readonly IFullRef<IExtensionBlock> _targetExtensionBlock;

    public ContractExtensionBlockTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IExtensionBlock> targetExtensionBlock,
        IFullRef<IParameter> receiverParameter,
        ContractDirection contractDirection,
        TemplateMember<IMethod> template,
        IObjectReader templateArguments ) : base(
        aspectLayerInstance,
        receiverParameter,
        contractDirection,
        template,
        templateArguments )
    {
        this._targetExtensionBlock = targetExtensionBlock;
    }

    public override IFullRef<IMemberOrNamedType> TargetMemberOrNamedType => this._targetExtensionBlock;

    public override IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context )
    {
        // Use GetTarget to get the extension block from the final compilation.
        var extensionBlock = this._targetExtensionBlock.GetTarget( context.FinalCompilation );
        var receiverParameter = extensionBlock.ReceiverParameter;
        var receiverExpression = SyntaxFactoryEx.SafeIdentifierName( receiverParameter.Name );
        var statements = new List<InsertedStatement>();

        // Determine effective direction. For Default, use the parameter's ref kind:
        // - Non-ref: Input
        // - Ref: Both
        // - Out: Output
        var effectiveDirection = this.ContractDirection;

        if ( effectiveDirection == ContractDirection.Default )
        {
            effectiveDirection = receiverParameter.RefKind switch
            {
                RefKind.Ref => ContractDirection.Both,
                RefKind.Out => ContractDirection.Output,
                _ => ContractDirection.Input
            };
        }

        // Generate statements for each instance member.
        // Each statement uses the method's first parameter (or method itself) as ContextDeclaration
        // to allow proper routing via IsContainedIn. The ordering logic checks ParentTransformation
        // to identify receiver parameter contracts.
        foreach ( var method in extensionBlock.Methods.Where( m => !m.IsStatic ) )
        {
            this.AddStatementsForMethod( context, method, receiverParameter, receiverExpression, effectiveDirection, statements );
        }

        foreach ( var property in extensionBlock.Properties.Where( p => !p.IsStatic ) )
        {
            if ( property.GetMethod != null )
            {
                this.AddStatementsForMethod( context, property.GetMethod, receiverParameter, receiverExpression, effectiveDirection, statements );
            }

            if ( property.SetMethod != null )
            {
                this.AddStatementsForMethod( context, property.SetMethod, receiverParameter, receiverExpression, effectiveDirection, statements );
            }
        }

        foreach ( var indexer in extensionBlock.Indexers.Where( i => !i.IsStatic ) )
        {
            if ( indexer.GetMethod != null )
            {
                this.AddStatementsForMethod( context, indexer.GetMethod, receiverParameter, receiverExpression, effectiveDirection, statements );
            }

            if ( indexer.SetMethod != null )
            {
                this.AddStatementsForMethod( context, indexer.SetMethod, receiverParameter, receiverExpression, effectiveDirection, statements );
            }
        }

        return statements;
    }

    private void AddStatementsForMethod(
        InsertStatementTransformationContext context,
        IMethod method,
        IParameter receiverParameter,
        ExpressionSyntax receiverExpression,
        ContractDirection effectiveDirection,
        List<InsertedStatement> statements )
    {
        // Use a parameter or return parameter that is contained in this method, so that
        // the linker can route the statement using IsContainedIn.
        // - For methods with parameters: use the first parameter
        // - For parameterless methods (like property getters): use the return parameter
        var routingDeclaration = method.Parameters.FirstOrDefault() as IDeclaration ?? method.ReturnParameter;

        if ( effectiveDirection is ContractDirection.Input or ContractDirection.Both )
        {
            Invariant.Assert( receiverParameter.RefKind is not RefKind.Out );

            if ( this.TryExecuteTemplate( context, receiverExpression, receiverParameter.Type, method, out var inputContractBlock ) )
            {
                statements.Add( new InsertedStatement( inputContractBlock, routingDeclaration, this, InsertedStatementKind.InputContract ) );
            }
        }

        if ( effectiveDirection is ContractDirection.Output or ContractDirection.Both )
        {
            Invariant.Assert( receiverParameter.RefKind is not RefKind.None );

            if ( this.TryExecuteTemplate( context, receiverExpression, receiverParameter.Type, method, out var outputContractBlock ) )
            {
                // For output contracts, use the return parameter for routing
                statements.Add( new InsertedStatement( outputContractBlock, method.ReturnParameter, this, InsertedStatementKind.OutputContract ) );
            }
        }
    }

    public override FormattableString ToDisplayString()
        => $"Add contract to receiver parameter of extension block '{this._targetExtensionBlock}'";
}
