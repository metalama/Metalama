// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerInjectionStep
{
    private sealed class InsertStatementTransformationContextImpl : InsertStatementTransformationContext
    {
        // ReSharper disable once MemberCanBePrivate.Local

        /// <summary>
        /// Gets the member for which this context exists.
        /// </summary>
        public IFullRef<IMember> ContextMember { get; }

        /// <summary>
        /// Gets the first transformation that uses this context.
        /// </summary>
        public IInsertStatementTransformation OriginTransformation { get; }

        public string? ReturnValueVariableName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the context was used for any input contract statements.
        /// </summary>
        public bool WasUsedForInputContracts { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the context was used for any output contract statements.
        /// </summary>
        public bool WasUsedForOutputContracts { get; private set; }

        public InsertStatementTransformationContextImpl(
            ProjectServiceProvider serviceProvider,
            UserDiagnosticSink diagnosticSink,
            SyntaxGenerationContext syntaxGenerationContext,
            CompilationModel compilation,
            ITemplateLexicalScopeProvider lexicalScopeProvider,
            IInsertStatementTransformation originTransformation,
            IFullRef<IMember> contextMember ) : base( serviceProvider, diagnosticSink, syntaxGenerationContext, compilation, lexicalScopeProvider )
        {
            this.ContextMember = contextMember;
            this.OriginTransformation = originTransformation;
        }

        public override string GetReturnValueVariableName()
        {
            var lexicalScope = this.LexicalScopeProvider.GetLexicalScope( this.ContextMember );

            return this.ReturnValueVariableName ??= lexicalScope.GetUniqueIdentifier( "returnValue" );
        }

        public void MarkAsUsedForOutputContracts() => this.WasUsedForOutputContracts = true;

        public void MarkAsUsedForInputContracts() => this.WasUsedForInputContracts = true;

        internal void Complete()
        {
            if ( this.WasUsedForOutputContracts )
            {
                // Allocate return value name if there was any output contract.
                // This is to have the return variable allocated even though we may not use it in some cases.
                // Doing this later would cause the variable name order to be strange.

                _ = this.GetReturnValueVariableName();
            }
        }
    }
}