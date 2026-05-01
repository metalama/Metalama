// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed class TemplateDriver
    {
        private readonly UserCodeInvoker _userCodeInvoker;
        private readonly MethodInfo _templateMethod;

        public TemplateDriver(
            ProjectServiceProvider serviceProvider,
            MethodInfo compiledTemplateMethodInfo )
        {
            this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
            this._templateMethod = compiledTemplateMethodInfo ?? throw new ArgumentNullException( nameof(compiledTemplateMethodInfo) );
        }

        internal static void CopyTemplateArguments( object?[] source, object?[] target, int index, SyntaxGenerationContext context )
        {
            for ( var i = 0; i < source.Length; i++ )
            {
                target[i + index] = source[i] switch
                {
                    TemplateTypeArgumentFactory factory => factory.Create( context ),
                    IPromise deferred => deferred.Value,
                    _ => source[i]
                };
            }
        }

        public bool TryExpandDeclaration(
            TemplateExpansionContext templateExpansionContext,
            object?[] templateArguments,
            [NotNullWhen( true )] out BlockSyntax? block )
        {
            templateExpansionContext.CheckTemplateLanguageVersion( templateExpansionContext, templateExpansionContext.MetaApi.Template );

            var errorCountBefore = templateExpansionContext.DiagnosticSink.ErrorCount;

            // Add the first template argument.
            var allArguments = new object?[templateArguments.Length + 1];
            allArguments[0] = templateExpansionContext.SyntaxFactory;

            // Add other arguments.
            CopyTemplateArguments( templateArguments, allArguments, 1, templateExpansionContext.SyntaxGenerationContext );

            if ( !this._userCodeInvoker.TryInvoke(
                    () => (SyntaxNode) this.InvokeTemplate( templateExpansionContext.TemplateProvider.Object, allArguments ).AssertNotNull(),
                    templateExpansionContext,
                    out var output ) )
            {
                block = null;

                return false;
            }

            var errorCountAfter = templateExpansionContext.DiagnosticSink.ErrorCount;

            block = (BlockSyntax) new FlattenBlocksRewriter().Visit( output )!;

            // Check if the expanded template contains yield statements inside try-catch blocks,
            // which is not allowed by C# (e.g., when a normal template with try-catch targets an async iterator method).
            if ( !templateExpansionContext.CheckYieldInTryCatch( block ) )
            {
                block = null;

                return false;
            }

            // If we're generating an async iterator method, but there is no yield statement, we would get an error.
            // Prevent that by adding `yield break;` at the end of the method body.
            block = templateExpansionContext.AddYieldBreakIfNecessary( block );

            block = block.NormalizeWhitespaceIfNecessary( templateExpansionContext.SyntaxGenerationContext );

            // We add generated-code annotations to the statements and not to the block itself so that the brackets don't get colored.
            var aspectClass = templateExpansionContext.MetaApi.AspectInstance?.AspectClass;
            block = block.WithGeneratedCodeAnnotation( aspectClass?.GeneratedCodeAnnotation ?? FormattingAnnotations.SystemGeneratedCodeAnnotation );

            return errorCountAfter == errorCountBefore;
        }

        internal object? InvokeTemplate( object? target, object?[] arguments )
        {
            // Pre-flight: if the target instance isn't assignable to the method's declaring type, the upcoming
            // MethodBase.Invoke will throw "Object does not match target type" with no diagnostic detail. That
            // almost always means two physically distinct copies of the same logical assembly are loaded — the
            // declaring type and the runtime type carry the same name but different assembly identity. Surface
            // the assembly identity and location of both sides so the load-context conflict can be diagnosed.
            var declaringType = this._templateMethod.DeclaringType;

            if ( target != null && declaringType != null && !declaringType.IsInstanceOfType( target ) )
            {
                var actualType = target.GetType();

                throw new InvalidOperationException(
                    $"Cannot invoke template method '{this._templateMethod.Name}' on the supplied target: the object's type is not assignable to the method's declaring type. "
                    + "This usually means two distinct copies of the same logical assembly are loaded into the process."
                    + Environment.NewLine
                    + $"  Method:             {this._templateMethod}" + Environment.NewLine
                    + $"  Declaring type:     {declaringType.AssemblyQualifiedName}" + Environment.NewLine
                    + $"  Declaring assembly: {declaringType.Assembly.FullName}" + Environment.NewLine
                    + $"  Declaring location: {declaringType.Assembly.GetLocationSafe()}" + Environment.NewLine
                    + $"  Actual type:        {actualType.AssemblyQualifiedName}" + Environment.NewLine
                    + $"  Actual assembly:    {actualType.Assembly.FullName}" + Environment.NewLine
                    + $"  Actual location:    {actualType.Assembly.GetLocationSafe()}" );
            }

            return this._templateMethod.Invoke( target, arguments );
        }
    }
}