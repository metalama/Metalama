# Metalama.Framework.DesignTime.Rpc

This project contains the RPC API between the processes of one Metalama version. It makes no promise of compatibility between
Metalama versions, so no consumer outside this repository may reference it: Metalama.Vsx talks to Metalama only through
`Metalama.Framework.DesignTime.Contracts`. See `Metalama.Framework/docs/cross-process-communication.md`.