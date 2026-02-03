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

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal sealed class ContractIndexerTransformation : ContractBaseTransformation
{
    private readonly IFullRef<IIndexer> _targetIndexer;

    public ContractIndexerTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IIndexer> targetIndexer,
        IFullRef<IParameter>? indexerParameter,
        ContractDirection contractDirection,
        TemplateMember<IMethod> template,
        IObjectReader templateArguments ) : base(
        aspectLayerInstance,
        (IFullRef<IDeclaration>?) indexerParameter ?? targetIndexer,
        contractDirection,
        template,
        templateArguments )
    {
        this._targetIndexer = targetIndexer;
    }

    public override IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context )
    {
        var targetIndexer = this._targetIndexer.GetTarget( context.FinalCompilation );
        var targetDeclaration = this.ContractTarget.GetTarget( this.InitialCompilation );

        switch ( targetDeclaration?.DeclarationKind )
        {
            case DeclarationKind.Indexer:
                {
                    Invariant.Assert( this.ContractTarget.Equals( this.TargetMemberOrNamedType ) );

                    Invariant.Assert( this.ContractDirection is ContractDirection.Output or ContractDirection.Input or ContractDirection.Both );

                    bool? inputResult, outputResult;
                    BlockSyntax? inputContractBlock, outputContractBlock;

                    if ( this.ContractDirection is ContractDirection.Input or ContractDirection.Both )
                    {
                        Invariant.Assert( targetIndexer.SetMethod is not null );

                        inputResult = this.TryExecuteTemplate(
                            context,
                            SyntaxFactoryEx.SafeIdentifierName( "value" ),
                            targetIndexer.Type,
                            targetIndexer.SetMethod,
                            out inputContractBlock );
                    }
                    else
                    {
                        inputResult = null;
                        inputContractBlock = null;
                    }

                    if ( this.ContractDirection is ContractDirection.Output or ContractDirection.Both )
                    {
                        Invariant.Assert( targetIndexer.GetMethod is not null );

                        var returnVariableName = context.GetReturnValueVariableName();

                        outputResult = this.TryExecuteTemplate(
                            context,
                            SyntaxFactoryEx.SafeIdentifierName( returnVariableName ),
                            targetIndexer.Type,
                            targetIndexer.GetMethod,
                            out outputContractBlock );
                    }
                    else
                    {
                        outputResult = null;
                        outputContractBlock = null;
                    }

                    if ( inputResult == false || outputResult == false )
                    {
                        return Array.Empty<InsertedStatement>();
                    }

                    var statements = new List<InsertedStatement>();

                    if ( inputContractBlock != null )
                    {
                        statements.Add(
                            new InsertedStatement(
                                inputContractBlock,
                                targetIndexer.SetMethod.AssertNotNull().Parameters[^1],
                                this,
                                InsertedStatementKind.InputContract ) );
                    }

                    if ( outputContractBlock != null )
                    {
                        statements.Add(
                            new InsertedStatement(
                                outputContractBlock,
                                targetIndexer.GetMethod.AssertNotNull().ReturnParameter,
                                this,
                                InsertedStatementKind.OutputContract ) );
                    }

                    return statements;
                }

            case DeclarationKind.Parameter when targetDeclaration is IParameter parameter:
                {
                    Invariant.Assert( this.ContractDirection is ContractDirection.Output or ContractDirection.Input or ContractDirection.Both );

                    bool? inputResult, outputResult;
                    BlockSyntax? inputContractBlock, outputContractBlock;
                    var valueSyntax = SyntaxFactoryEx.SafeIdentifierName( parameter.Name );

                    if ( this.ContractDirection is ContractDirection.Input or ContractDirection.Both )
                    {
                        Invariant.Assert( parameter.RefKind is not RefKind.Out );

                        inputResult = this.TryExecuteTemplate(
                            context,
                            valueSyntax,
                            parameter.Type,
                            targetIndexer.GetMethod.AssertNotNull(),
                            out inputContractBlock );
                    }
                    else
                    {
                        inputResult = null;
                        inputContractBlock = null;
                    }

                    if ( this.ContractDirection is ContractDirection.Output or ContractDirection.Both )
                    {
                        Invariant.Assert( parameter.RefKind is not RefKind.None );

                        outputResult = this.TryExecuteTemplate(
                            context,
                            valueSyntax,
                            parameter.Type,
                            targetIndexer.SetMethod.AssertNotNull(),
                            out outputContractBlock );
                    }
                    else
                    {
                        outputResult = null;
                        outputContractBlock = null;
                    }

                    if ( inputResult == false || outputResult == false )
                    {
                        return Array.Empty<InsertedStatement>();
                    }

                    var statements = new List<InsertedStatement>();

                    if ( inputContractBlock != null )
                    {
                        statements.Add( new InsertedStatement( inputContractBlock, parameter, this, InsertedStatementKind.InputContract ) );
                    }

                    if ( outputContractBlock != null )
                    {
                        statements.Add( new InsertedStatement( outputContractBlock, parameter, this, InsertedStatementKind.OutputContract ) );
                    }

                    return statements;
                }

            default:
                throw new AssertionFailedException( $"Unsupported contract target: {this.ContractTarget}" );
        }
    }

    public override IFullRef<IMemberOrNamedType> TargetMemberOrNamedType => this._targetIndexer;

    public override FormattableString ToDisplayString()
    {
        if ( this.ContractTarget.GetTarget( this.InitialCompilation )?.DeclarationKind == DeclarationKind.Indexer )
        {
            return $"Add contract to indexer '{this._targetIndexer}'";
        }

        return base.ToDisplayString();
    }
}