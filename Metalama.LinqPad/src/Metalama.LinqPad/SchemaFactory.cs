// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using LINQPad.Extensibility.DataContext;
using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.LinqPad;

internal sealed class SchemaFactory
{
    private readonly Func<Type, bool, string> _formatTypeFunc;

    public SchemaFactory( Func<Type, bool, string> formatTypeFunc )
    {
        this._formatTypeFunc = formatTypeFunc;
    }

    public List<ExplorerItem> GetSchema( string workspaceExpression, Workspace? workspace = null )
    {
        var isMetalamaEnabled = workspace != null && workspace.Projects.Any( x => x.IsMetalamaEnabled );

        var workspaceSchema = this.GetProjectSetSchema( workspaceExpression, p => p.Name != nameof(IProjectSet.Projects), isMetalamaEnabled, true );

        if ( workspace != null )
        {
            var rootSchema = new List<ExplorerItem>();

            var workspaceItem = new ExplorerItem( "Workspace", ExplorerItemKind.QueryableObject, ExplorerIcon.Schema )
            {
                DragText = workspaceExpression, ToolTipText = "Query all projects loaded in the workspace as a whole.", Children = [..workspaceSchema]
            };

            rootSchema.Add( workspaceItem );

            var projectsItem = new ExplorerItem( "Projects", ExplorerItemKind.QueryableObject, ExplorerIcon.Table )
            {
                DragText = $"{workspaceExpression}.Projects", ToolTipText = "Query individual projects of the workspace", IsEnumerable = true, Children = []
            };

            rootSchema.Add( projectsItem );

            foreach ( var project in workspace.Projects )
            {
                var nameLiteral = SyntaxFactory.Literal( project.Name ).Text;
                var frameworkLiteral = SyntaxFactory.Literal( project.TargetFramework ).Text;

                var prefix = $"{workspaceExpression}.GetProject({nameLiteral}, {frameworkLiteral})";

                var projectItem =
                    new ExplorerItem( project.ToString(), ExplorerItemKind.QueryableObject, ExplorerIcon.Box )
                    {
                        DragText = prefix, ToolTipText = $"Query the project '{project}'."
                    };

                var projectSchema = this.GetProjectSetSchema( prefix, p => p.Name != nameof(IProjectSet.Projects), project.IsMetalamaEnabled, false );
                projectItem.Children = [..projectSchema];

                projectsItem.Children.Add( projectItem );
            }

            return rootSchema;
        }
        else
        {
            return workspaceSchema;
        }
    }

    private List<ExplorerItem> GetProjectSetSchema( string prefix, Func<PropertyInfo, bool> filterProperty, bool isMetalamaEnabled, bool isWorkspace )
    {
        bool IsMetalamaProperty( PropertyInfo p ) => p.Name != nameof(IIntrospectionCompilationDetails.Diagnostics);

        bool IsIncludedInCodeModel( PropertyInfo p )
            => isWorkspace || (p.Name != nameof(IProjectSet.Projects) && p.Name != nameof(ICompilationSet.Compilations)
                                                                      && p.Name != nameof(ICompilationSet.TargetFrameworks));

        var workspaceSchema = this.GetSchema(
            prefix + ".",
            typeof(IProjectSet),
            p => p.Name is not (nameof(IProjectSet.SourceCode) or nameof(IProjectSet.TransformedCode))
                 && !(p.DeclaringType == typeof(IIntrospectionCompilationDetails) && IsMetalamaProperty( p )) && filterProperty( p ) );

        if ( isMetalamaEnabled )
        {
            var sourceCodeItem =
                new ExplorerItem( "Source Code", ExplorerItemKind.Property, ExplorerIcon.Schema )
                {
                    Children = this.GetSchema( $"{prefix}.{nameof(Workspace.SourceCode)}.", typeof(ICompilationSet), IsIncludedInCodeModel )
                };

            workspaceSchema.Insert( 0, sourceCodeItem );

            var transformedCodeItem =
                new ExplorerItem( "Transformed Code", ExplorerItemKind.Property, ExplorerIcon.Schema )
                {
                    Children = this.GetSchema( $"{prefix}.{nameof(Workspace.TransformedCode)}.", typeof(ICompilationSet), IsIncludedInCodeModel )
                };

            workspaceSchema.Insert( 1, transformedCodeItem );

            var metalamaItem =
                new ExplorerItem( "Aspects", ExplorerItemKind.Property, ExplorerIcon.Schema )
                {
                    Children = this.GetSchema( $"{prefix}.", typeof(IIntrospectionCompilationDetails), IsMetalamaProperty )
                };

            workspaceSchema.Insert( 2, metalamaItem );
        }
        else
        {
            workspaceSchema.InsertRange( 0, this.GetSchema( $"{prefix}.{nameof(Workspace.SourceCode)}.", typeof(ICompilationSet), IsIncludedInCodeModel ) );
        }

        return workspaceSchema;
    }

    private List<ExplorerItem> GetSchema( string prefix, Type type, Func<PropertyInfo, bool> isIncluded )
    {
        // Return the objects with which to populate the Schema Explorer by reflecting over customType.

        // We'll start by retrieving all the properties of the custom type that implement IEnumerable<T>:
        var topLevelProps =
        (
            from property in GetProperties( type, isIncluded )
            where property.PropertyType != typeof(string)
            let enumerableType = GetIEnumerable( property.PropertyType ).FirstOrDefault()
            where enumerableType != null
            orderby property.Name
            select new ExplorerItem( property.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table )
            {
                IsEnumerable = true,
                DragText = prefix + property.Name,
                ToolTipText = this.FormatTypeName( property.PropertyType, false ),

                // Store the entity type to the Tag property. We'll use it later.
                Tag = enumerableType.GetGenericArguments()[0]
            }
        ).ToMutableList();

        // Create a lookup keying each element type to the properties of that type. This will allow
        // us to build hyperlink targets allowing the user to click between associations:
        var elementTypeLookup = topLevelProps.ToLookup( tp => (Type) tp.Tag );

        // Populate the columns (properties) of each entity:
        foreach ( var table in topLevelProps )
        {
            var parentType = (Type) table.Tag;

            var props = GetProperties( parentType, _ => true )
                .OrderBy( p => (p.Name, p.PropertyType), PropertyComparer.Instance )
                .Select( p => this.GetChildItem( elementTypeLookup, p.Name, p.PropertyType ) );

            table.Children = props.ToMutableList();

            // If we have a declaration, populate reference extension methods.
            if ( parentType.IsAssignableTo( typeof(IDeclaration) ) )
            {
                table.Children.Add(
                    new ExplorerItem( "GetInboundReferences()", ExplorerItemKind.ReferenceLink, ExplorerIcon.TableFunction )
                    {
                        IsEnumerable = true, DragText = prefix + "GetInboundReferences()"
                    } );

                table.Children.Add(
                    new ExplorerItem( "GetOutboundReferences()", ExplorerItemKind.ReferenceLink, ExplorerIcon.TableFunction )
                    {
                        IsEnumerable = true, DragText = prefix + "GetOutboundReferences()"
                    } );
            }
        }

        return topLevelProps;
    }

    private static IEnumerable<Type> GetIEnumerable( Type type )
    {
        if ( type == typeof(string) )
        {
            return [];
        }
        else if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) )
        {
            return new[] { type };
        }
        else
        {
            return type.GetInterfaces().Where( t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>) );
        }
    }

    private ExplorerItem GetChildItem( ILookup<Type, ExplorerItem> elementTypeLookup, string childPropName, Type childPropType )
    {
        // If the property's type is in our list of entities, then it's a Many:1 (or 1:1) reference.
        // We'll assume it's a Many:1 (we can't reliably identify 1:1s purely from reflection).
        if ( childPropType != typeof(string) && elementTypeLookup.Contains( childPropType ) )
        {
            return new ExplorerItem( childPropName, ExplorerItemKind.ReferenceLink, ExplorerIcon.ManyToOne )
            {
                HyperlinkTarget = elementTypeLookup[childPropType].First(),

                // FormatTypeName is a helper method that returns a nicely formatted type name.
                ToolTipText = this.FormatTypeName( childPropType, true )
            };
        }

        // Is the property's type a collection of entities?
        var enumerableType = GetIEnumerable( childPropType ).FirstOrDefault();

        if ( enumerableType != null )
        {
            var elementType = enumerableType.GetGenericArguments()[0];

            if ( elementTypeLookup.Contains( elementType ) )
            {
                return new ExplorerItem( childPropName, ExplorerItemKind.CollectionLink, ExplorerIcon.OneToMany )
                {
                    HyperlinkTarget = elementTypeLookup[elementType].First(),
                    ToolTipText = this.FormatTypeName( elementType, true ),
                    DragText = childPropName
                };
            }
        }

        // Ordinary property:
        return new ExplorerItem(
            childPropName + " (" + this.FormatTypeName( childPropType, false ) + ")",
            ExplorerItemKind.Property,
            ExplorerIcon.Column ) { DragText = childPropName };
    }

    private static IReadOnlyList<PropertyInfo> GetProperties( Type type, Func<PropertyInfo, bool> filter )
    {
        if ( type == typeof(string) )
        {
            return Array.Empty<PropertyInfo>();
        }
        else if ( type.IsInterface )
        {
            var properties = new Dictionary<string, PropertyInfo>();

            // Properties defined in the top interface win.
            foreach ( var property in type.GetProperties() )
            {
                if ( filter( property ) )
                {
                    properties[property.Name] = property;
                }
            }

            // For base interfaces, we don't know which ones are the deepest, so the result is random.
            foreach ( var property in type.GetInterfaces().SelectMany( i => i.GetProperties() ) )
            {
                if ( filter( property ) )
                {
                    properties[property.Name] = property;
                }
            }

            return properties.Values.ToArray();
        }
        else
        {
            return type.GetProperties().Where( filter ).ToReadOnlyList();
        }
    }

    private string FormatTypeName( Type type, bool includeNamespace ) => this._formatTypeFunc( type, includeNamespace );
}