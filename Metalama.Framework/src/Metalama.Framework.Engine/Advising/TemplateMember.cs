// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Linq;
using Accessibility = Metalama.Framework.Code.Accessibility;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.Advising;

internal abstract class TemplateMember
{
    internal abstract ISymbolRef<IMemberOrNamedType> GetDeclarationRef();

    protected CompilationModel GetTemplateReflectionCompilation( CompilationModel compilationModel )
        => this.TemplateClassMember.TemplateClass.GetTemplateReflectionCompilation( compilationModel );

    public TemplateClassMember TemplateClassMember { get; }

    public ISymbol Symbol => this.GetDeclarationRef().Symbol;

    public RefFactory RefFactory => this.GetDeclarationRef().RefFactory;

    // Can be null in the default instance.
    public IAdviceAttribute? AdviceAttribute { get; }

    public TemplateKind SelectedTemplateKind { get; }

    /// <summary>
    /// Gets a value indicating which kind should the template be interpreted as, based on the target method.
    /// For example, a <see cref="TemplateKind.Default"/> template that is applied to an <c>async Task</c> method should be interpreted as <see cref="TemplateKind.Async"/>.
    /// </summary>
    public TemplateKind InterpretedTemplateKind { get; }

    public Accessibility Accessibility { get; }

    public Accessibility GetAccessorAccessibility { get; }

    public Accessibility SetAccessorAccessibility { get; }

    public IObjectReader Tags { get; }

    /// <summary>
    /// Gets a value indicating whether the method is a <c>yield</c>-based iterator method. If the template is a property, the value applies to the getter.
    /// </summary>
    public bool IsIteratorMethod { get; }

    /// <summary>
    /// Gets a value indicating which kind should the template be treated as, based on the selected template method.
    /// For example, a <see cref="TemplateKind.Default"/> template that is <c>async Task</c> should be treated as <see cref="TemplateKind.Async"/>.
    /// </summary>
    public TemplateKind EffectiveTemplateKind { get; }

    public TemplateProvider TemplateProvider { get; }

    private TemplateKind GetEffectiveKind( ISymbol declaration )
    {
        if ( this.SelectedTemplateKind == TemplateKind.Default )
        {
            if ( declaration is IMethodSymbol method && method.IsAsyncSafe() )
            {
                if ( this.IsIteratorMethod )
                {
                    switch ( method.GetEnumerableKind() )
                    {
                        case EnumerableKind.IAsyncEnumerable:
                            return TemplateKind.IAsyncEnumerable;

                        case EnumerableKind.IAsyncEnumerator:
                            return TemplateKind.IAsyncEnumerator;
                    }
                }
                else
                {
                    return TemplateKind.Async;
                }
            }
            else if ( this.IsIteratorMethod )
            {
                var iteratorMethod = declaration as IMethodSymbol ?? (declaration as IPropertySymbol)?.GetMethod;

                switch ( iteratorMethod?.GetEnumerableKind() )
                {
                    case EnumerableKind.IEnumerable:
                        return TemplateKind.IEnumerable;

                    case EnumerableKind.IEnumerator:
                        return TemplateKind.IEnumerator;
                }
            }
        }

        return this.SelectedTemplateKind;
    }

    [Memo]
    public TemplateDriver Driver => this.TemplateClassMember.TemplateClass.GetTemplateDriver( this.GetDeclarationRef() );

    protected TemplateMember( TemplateMember prototype )
    {
        this.Accessibility = prototype.Accessibility;
        this.InterpretedTemplateKind = prototype.InterpretedTemplateKind;
        this.Accessibility = prototype.Accessibility;
        this.AdviceAttribute = prototype.AdviceAttribute;
        this.IsIteratorMethod = prototype.IsIteratorMethod;
        this.EffectiveTemplateKind = prototype.EffectiveTemplateKind;
        this.SelectedTemplateKind = prototype.SelectedTemplateKind;
        this.GetAccessorAccessibility = prototype.GetAccessorAccessibility;
        this.SetAccessorAccessibility = prototype.SetAccessorAccessibility;
        this.TemplateClassMember = prototype.TemplateClassMember;
        this.TemplateProvider = prototype.TemplateProvider;
        this.Tags = prototype.Tags;
    }

    protected TemplateMember(
        ISymbolRef<IMemberOrNamedType> implementation,
        TemplateClassMember templateClassMember,
        TemplateProvider templateProvider,
        IAdviceAttribute adviceAttribute,
        IObjectReader tags,
        TemplateKind selectedTemplateKind,
        TemplateKind interpretedTemplateKind )
    {
        var symbol = implementation.Symbol;

        this.TemplateClassMember = templateClassMember;
        this.TemplateProvider = templateProvider;
        this.AdviceAttribute = adviceAttribute.AssertNotNull();
        this.Tags = tags;

        if ( symbol is IMethodSymbol { MethodKind: MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove }
             && templateClassMember.Parameters.Length != 1 )
        {
            throw new AssertionFailedException(
                $"'{symbol}' is an accessor but the template '{templateClassMember.Name}' has {templateClassMember.Parameters.Length} parameters." );
        }

        // Get the template characteristics that may disappear or be changed during template compilation.
        var compiledTemplateAttribute = GetCompiledTemplateAttribute( symbol );

        // Set the accessibility.
        // The one defined on the [Template] attribute has priority, then on [Accessibility],
        // then the accessibility of the template itself.
        if ( adviceAttribute is ITemplateAttribute { Properties.Accessibility: { } templateAccessibility } )
        {
            this.Accessibility = templateAccessibility;
        }
        else
        {
            this.Accessibility = compiledTemplateAttribute.Accessibility;
            this.IsIteratorMethod = compiledTemplateAttribute.IsIteratorMethod;
        }

        if ( symbol is IPropertySymbol property )
        {
            if ( property.GetMethod != null )
            {
                var attributeOnGetter = GetCompiledTemplateAttribute( property.GetMethod );
                this.GetAccessorAccessibility = attributeOnGetter.Accessibility;
                this.IsIteratorMethod = attributeOnGetter.IsIteratorMethod;
            }

            if ( property.SetMethod != null )
            {
                this.SetAccessorAccessibility = GetCompiledTemplateAttribute( property.SetMethod ).Accessibility;
            }
        }

        // Set the template kind.
        this.SelectedTemplateKind = selectedTemplateKind;
        this.InterpretedTemplateKind = interpretedTemplateKind != TemplateKind.None ? interpretedTemplateKind : selectedTemplateKind;
        this.EffectiveTemplateKind = this.GetEffectiveKind( symbol );
    }

    private static CompiledTemplateAttribute GetCompiledTemplateAttribute( ISymbol? declaration )
    {
        var attribute = new CompiledTemplateAttribute() { Accessibility = Accessibility.Private };

        if ( declaration == null )
        {
            return attribute;
        }

        // Set the attribute data with data computed from the code, if available.
        attribute.Accessibility = declaration.DeclaredAccessibility.ToOurAccessibility();

        if ( declaration is IMethodSymbol method )
        {
            attribute.IsIteratorMethod = method.IsIteratorMethod();
            attribute.IsAsync = method.IsAsyncSafe();
        }

        // Override with values stored in the CompiledTemplateAttribute.
        var attributeData = declaration.GetAttributes().SingleOrDefault( a => a.AttributeClass?.Name == nameof(CompiledTemplateAttribute) );

        if ( attributeData == null )
        {
            return attribute;
        }

        if ( attributeData.TryGetNamedArgument( nameof(CompiledTemplateAttribute.Accessibility), out var accessibility ) )
        {
            attribute.Accessibility = ((Microsoft.CodeAnalysis.Accessibility) accessibility.Value!).ToOurAccessibility();
        }

        if ( attributeData.TryGetNamedArgument( nameof(CompiledTemplateAttribute.IsAsync), out var isAsync ) )
        {
            attribute.IsAsync = (bool) isAsync.Value!;
        }

        if ( attributeData.TryGetNamedArgument( nameof(CompiledTemplateAttribute.IsIteratorMethod), out var isIterator ) )
        {
            attribute.IsIteratorMethod = (bool) isIterator.Value!;
        }

        return attribute;
    }

    public TemplateMember<IMemberOrNamedType> AsMemberOrNamedType()
        => this as TemplateMember<IMemberOrNamedType> ?? new TemplateMember<IMemberOrNamedType>( this );

    public TemplateMember<TOther> As<TOther>()
        where TOther : class, IMemberOrNamedType
        => this as TemplateMember<TOther> ?? new TemplateMember<TOther>( this );

    public override string ToString() => $"{this.GetDeclarationRef().Name}:{this.SelectedTemplateKind}";
}