// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Formatting
{
    /// <summary>
    /// Options influencing the HTML writing behavior of the test framework.
    /// </summary>
    /// <param name="AddTitles"></param>
    /// <param name="Prolog"></param>
    /// <param name="Epilogue"></param>
    public sealed record HtmlCodeWriterOptions( bool AddTitles = false, string? Prolog = null, string? Epilogue = null );
}