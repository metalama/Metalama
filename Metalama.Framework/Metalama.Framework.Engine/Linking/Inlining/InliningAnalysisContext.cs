// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class InliningAnalysisContext
{
    private readonly IdGenerator _idGenerator;

    public bool UsingSimpleInlining { get; }

    public string? ReturnVariableIdentifier { get; }

    public InliningId Id { get; }

    public InliningId? ParentId { get; }

    public InliningAnalysisContext() : this( null, new IdGenerator(), true, null ) { }

    private InliningAnalysisContext( InliningId? parentId, IdGenerator identifierProvider, bool usingSimpleInlining, string? returnVariableIdentifier )
    {
        this.UsingSimpleInlining = usingSimpleInlining;
        this._idGenerator = identifierProvider;
        this.Id = this._idGenerator.GetNextId();
        this.ParentId = parentId;
        this.ReturnVariableIdentifier = returnVariableIdentifier;
    }

    public string AllocateReturnLabel() => this._idGenerator.AllocateReturnLabel();

    internal InliningAnalysisContext Recurse() => new( this.Id, this._idGenerator, this.UsingSimpleInlining, null );

    internal InliningAnalysisContext RecurseWithSimpleInlining() => new( this.Id, this._idGenerator, true, null );

    internal InliningAnalysisContext RecurseWithComplexInlining( string? returnVariableIdentifier )
        => new( this.Id, this._idGenerator, false, returnVariableIdentifier );

    public override string ToString() => $"{{Id={this.Id}, ParentId={this.ParentId?.ToString() ?? "null"}, ReturnVariableIdentifier={this.ReturnVariableIdentifier ?? "null"}}}";

    private sealed class IdGenerator
    {
        private int _nextOrdinal = 1;
        private int _nextReturnLabelIdentifier = 1;

        public InliningId GetNextId() => new InliningId( this._nextOrdinal++ );

        public string AllocateReturnLabel()
        {
            var id = this._nextReturnLabelIdentifier++;

            return $"__aspect_return_{id}";
        }
    }
}