# GUID Rotation Audit Trail

This file documents the v1→v2 GUID rotation for `Metalama.Framework.DesignTime.Contracts`.
It is the audit trail referenced from the cross-version-communication design doc.

The 2026.1+ branches ship the v2 assembly (`<AssemblyName>Metalama.Framework.DesignTime.Contracts.v2</AssemblyName>`)
from this folder. The v1 surface remains frozen on the 2026.0.x release branches; consult their git history
(e.g. tag `release/2026.0.22`) for the v1 GUIDs listed in the table below.

Every `[Guid]`-marked type, plus the assembly-level `[assembly: Guid]` in `AssemblyInfo.cs`,
got a freshly generated GUID. Type names, namespaces, member signatures and ordering are byte-identical to v1.

DO NOT edit GUIDs in this file or in any source file in this folder: they are now frozen for the lifetime of v2.
Future contract evolution must go through a new interface (`IFooN`) with its own GUID, never by mutating an existing one.

## Mapping table

| File (relative to project root) | v1 GUID (frozen on 2026.0.x) | v2 GUID (this branch) |
|---|---|---|
| `AspectExplorer/AspectExplorerAspectInstance.cs` | `AF977B33-AA8C-4481-9B7E-B14A67200429` | `22139B0D-3397-4210-8F85-374A2D543DB0` |
| `AspectExplorer/AspectExplorerAspectInstance.cs` | `415F68C2-FFAD-4176-9062-53C3658E5F18` | `8A11CA4E-B57C-4A3F-861D-5BA93209D55A` |
| `AspectExplorer/AspectExplorerAspectInstance.cs` | `E758C91B-E335-4D53-AA30-82BBCCBF428A` | `C35DF59E-9359-4D8B-AF8A-6DA4F5540F00` |
| `AspectExplorer/AspectExplorerAspectInstance.cs` | `E0C881D8-C8FF-4988-B73D-CDEB6561CEBD` | `AFB6A223-4B40-4F75-B88D-01F1228A5187` |
| `AspectExplorer/AspectExplorerAspectInstance.cs` | `1E91F9F1-FD0E-4668-B4D1-6D445C7BE1FD` | `96F4689F-0FBA-4732-B7C3-069F608F79C2` |
| `AspectExplorer/IAspectDatabaseService.cs` | `C0BDC548-BC2D-40C6-B9A8-96FEB4CCEEBA` | `F9D7E67E-3AA9-4783-9682-5EA3672DC399` |
| `AspectExplorer/IAspectDatabaseService.cs` | `99E80D57-0C81-4461-B956-ECB1A7C3AA18` | `AD1C0745-4D95-4338-8565-090A69D0A306` |
| `AssemblyInfo.cs` | `de388915-dcfd-4fe2-aaa3-930247837f16` | `234D9C3E-29CA-4ACC-8DB5-3F0D5C931D41` |
| `Classification/DesignTimeClassifiedTextSpan.cs` | `114cc8b6-7363-438c-8742-f3076bd8afce` | `909FF874-5D3D-4405-B9AA-69781307BA28` |
| `Classification/DesignTimeTextSpanClassification.cs` | `5780f7d7-ee83-41c9-9568-c49d42171c93` | `7DA04C5D-73B4-4774-9316-27CCAB521B81` |
| `Classification/IClassificationService.cs` | `4BBF0FC6-7D08-4761-8C81-5AEDC838C6E7` | `B127AA55-6A16-40B7-94F3-DEDCCE90F9E1` |
| `Classification/IDesignTimeClassifiedTextCollection.cs` | `D498C406-2F33-4EFC-85FC-0B09CFD160F8` | `04715C22-4D7C-42B2-AC93-17CEC26B4397` |
| `CodeLens/ICodeLensDetails.cs` | `FFFB9B14-7D4A-4BC4-AD83-2495A9DC5AC0` | `C4E5917E-12CA-4589-88AE-65382A4BD7AC` |
| `CodeLens/ICodeLensDetailsEntry.cs` | `3903FF85-40C4-4158-9A38-CA5C9CC084CA` | `FD1127A6-1BA2-4548-97E5-24640C3E12BC` |
| `CodeLens/ICodeLensDetailsField.cs` | `AD813C57-3CB5-40D9-A553-D46A4790FCD5` | `4F3B7496-9B7E-4D96-93CA-6FB9EBA2A05C` |
| `CodeLens/ICodeLensDetailsHeader.cs` | `4BBCF97F-A51E-4D2D-A2BD-3C639FBACC80` | `F430FF8C-E4A7-44E6-AFB5-2FF476B1FEBD` |
| `CodeLens/ICodeLensDetailsTable.cs` | `1516E7C1-8076-4226-9999-C1C961E08E0A` | `9046938A-AF29-4AC2-AF46-20EF5818238A` |
| `CodeLens/ICodeLensService.cs` | `9E3E6194-302E-4F36-8612-FD2CA0190F21` | `35A231CD-EA5E-40CB-8CEF-5832C65C66B9` |
| `CodeLens/ICodeLensSummary.cs` | `90EE87E4-68CD-43FA-996F-FD0AE6691610` | `BC44DEB1-319B-45DA-9079-F7FF96E64353` |
| `Diagnostics/ICompileTimeErrorStatusService.cs` | `B3195FB8-73FF-47B9-9519-A50E2464A7F5` | `AA73EC87-55AD-4135-8728-AEC30F3E9BB4` |
| `Diagnostics/IDiagnosticData.cs` | `2D5AD05C-ED86-45CC-A9F2-5F6E8186AF7C` | `24F86F3F-E0A4-418B-A658-580DC7546409` |
| `EntryPoint/ContractVersion.cs` | `8A5841E3-5D21-495C-99D8-280558B3A7BD` | `A05522EE-D059-43C8-A073-50EAB6C8E1C6` |
| `EntryPoint/ICompilerService.cs` | `D174F35D-ABA7-4CDC-8B47-44E979019B3E` | `576D9B19-26B1-440C-AC58-51BBE980DD65` |
| `EntryPoint/ICompilerServiceProvider.cs` | `C5D68E3C-F7A7-428E-91FC-090AE7EBA023` | `06E79E6F-DD3E-4528-94FA-E776405377D0` |
| `EntryPoint/ICompileTimeEditingStatusService.cs` | `8BA9557E-2E58-4933-86D0-58C2043C4AE4` | `5AE7F683-A552-4436-93F6-D750E66F0F0A` |
| `EntryPoint/IDesignTimeEntryPointConsumer.cs` | `B6EAF9AE-2A70-4BBB-93A1-C877E2758462` | `3964BD63-E712-4146-BF17-0A6FA6B8E668` |
| `EntryPoint/IDesignTimeEntryPointManager.cs` | `A0C85506-DB96-4C14-86E8-5F199731534B` | `EA86D690-6249-43BC-9F1B-FE8E7AFDC375` |
| `EntryPoint/LogAction.cs` | `6B2FA7C5-65E9-4182-B8AA-96381CBBFF76` | `41C1BF88-393F-4A88-9655-F63DD2E14A20` |
| `EntryPoint/ServiceProviderEventHandler.cs` | `A774931E-EF64-44D0-BD02-957BD60B3CCF` | `73840882-EFA8-4EC8-93A3-5673567C204D` |
| `Notifications/ICompilationResultChangedEvent.cs` | `1C09E6CD-AF9C-4DD3-A431-0B5F3A59F77A` | `E1D66E33-12BC-40B3-A319-A80C112170A3` |
| `Notifications/IDesignTimeNotificationEvent.cs` | `89D91698-1380-430C-A42B-FE3BDC945961` | `9BAB53DF-7FED-4757-B589-54E79EEE5A20` |
| `Notifications/IDesignTimeNotificationObserver.cs` | `BD40B92D-7D10-4B54-8446-9756E9ECAC72` | `3CAE9F74-193F-4502-BD81-B0ABBEC44559` |
| `Notifications/IDesignTimeNotificationService.cs` | `E9CF1E1D-8BEA-4BC8-84AB-034C944B6E77` | `78E9D456-59D8-4350-A63E-6B88D439B82E` |
| `Notifications/IEndpointChangedEvent.cs` | `FB32D6B7-5D68-479D-966A-34070B38B1B6` | `A1F5F4CD-F810-4EDB-B2B8-7CADB82429BE` |
| `Pipeline/ITransitiveCompilationResult.cs` | `CDA98261-4BAD-4117-8054-49390BCBF4E6` | `2C0990E9-CDA8-453F-8614-7FA3F76EE1EE` |
| `Pipeline/ITransitiveCompilationService.cs` | `63D30200-2953-4967-BF65-8A693B26ED7E` | `6D9D7FF5-864A-492E-BE39-54112FB35BF5` |
| `Preview/IPreviewTransformationResult.cs` | `56DF8D75-6AA9-4669-976A-1BB79D5D783C` | `8608B071-D405-42F4-816F-49042E3321A7` |
| `Preview/ITransformationPreviewService.cs` | `982B62AD-5BB5-4B44-A7B2-2E3BEB19DE9E` | `7A873458-5842-4FA5-B67F-D056DB2F245C` |
| `Preview/ITransformationPreviewService.cs` | `2D800D48-3BF1-4EF8-98F5-62FA4417F3F7` | `1053A716-1A04-4B5E-AC27-3C2D6F3BBC9C` |
| `ServiceHub/IServiceHubInfo.cs` | `A3F04A92-6ECA-4861-956A-57AD6309C095` | `DF409271-4E82-439D-9BF9-7D8C93D43B31` |
| `ServiceHub/IServiceHubLocator.cs` | `B8DAD9AE-CF7F-4E70-863C-E434272023DD` | `B31B898F-3018-4F73-A1C6-87AE7EE44A02` |
| `Workspaces/IWorkspaceReceiver.cs` | `b2a4a92a-8b0f-45d4-bad7-bc5e9a370fc0` | `08778E4F-926A-4E5B-BE52-FB07644B22AC` |

## AppDomain slot

v1 uses `Metalama.Framework.DesignTime.Contracts.DesignTimeEntryPointManager`.
v2 uses `Metalama.Framework.DesignTime.Contracts.v2.DesignTimeEntryPointManager`.

The named mutex shares the same string. Both managers can coexist in the same AppDomain on independent slots.

Total: 42 GUIDs rotated across 36 files.
