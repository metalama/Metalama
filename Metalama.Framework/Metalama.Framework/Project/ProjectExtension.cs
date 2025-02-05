// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Project
{
    /// <summary>
    /// An base class that must be implemented by classes that want to extend <see cref="IProject"/> with project-local configuration data using
    /// the <see cref="IProject.Extension{T}"/> method.
    /// </summary>
    /// <remarks>
    /// The implementation must not allow modifications of the state after the object has been made read only.
    /// </remarks>
    /// <seealso href="@aspect-configuration"/>
    [CompileTime]
    [Obsolete( "Use IHierarchicalOptions." )]
    public abstract class ProjectExtension
    {
        // ReSharper disable UnusedParameter.Global

        /// <summary>
        /// Initializes the object from project properties.
        /// </summary>
        /// <param name="project">The project to which the new <see cref="ProjectExtension"/> belongs.</param>
        /// <param name="isReadOnly">A value indicating whether the project data is already read-only. If <c>false</c>, the project data
        /// can still be modified by project fabrics, after which the <see cref="MakeReadOnly"/> method will be called.</param>
        public virtual void Initialize( IProject project, bool isReadOnly ) { }

        // ReSharper restore UnusedParameter.Global

        /// <summary>
        /// Signals that further modifications of the object must be prevented.
        /// </summary>
        protected internal virtual void MakeReadOnly()
        {
            this.IsReadOnly = true;
        }

        public bool IsReadOnly { get; internal set; }
    }
}