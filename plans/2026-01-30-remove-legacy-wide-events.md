# Remove Legacy Wide Event System & Pipeline Improvements

## Status: In Progress

## Files to Delete
- [x] `src/VsaResults.Features/WideEvents/FeatureWideEvent.cs`
- [x] `src/VsaResults.Features/WideEvents/FeatureWideEventBuilder.cs`
- [x] `src/VsaResults.Features/WideEvents/IWideEventEmitter.cs`
- [x] `src/VsaResults.Features/WideEvents/NullWideEventEmitter.cs`
- [x] `src/VsaResults.Features/WideEvents/SerilogWideEventEmitter.cs`
- [x] `src/VsaResults.Features/WideEvents/Unified/Compatibility/LegacyWideEventEmitterAdapter.cs`
- [x] `src/VsaResults.Features/WideEvents/Unified/Compatibility/UnifiedToLegacyEmitterAdapter.cs`
- [x] `src/VsaResults.Features/WideEvents/Unified/Compatibility/LegacyEmitterSinkAdapter.cs`

## Files to Modify
- [x] `src/VsaResults.Features/WideEvents/Unified/IUnifiedWideEventEmitter.cs` - Rename interface to IWideEventEmitter, rename NullUnified to NullWideEventEmitter
- [x] `src/VsaResults.Features/Features/FeatureExecutionExtensions.cs` - Remove 2 legacy overloads, deduplicate, use EmitAsync, add cancellation checks, remove dead code, fix RecordException
- [x] `src/VsaResults.AspNetCore/AspNetCore/FeatureHandler.cs` - Switch to IWideEventEmitter (unified), fix bound handlers
- [x] `src/VsaResults.AspNetCore/AspNetCore/FeatureController.cs` - Switch to IWideEventEmitter (unified)
- [x] `src/VsaResults.Features/DependencyInjection/ServiceCollectionExtensions.cs` - Remove legacy methods, rename AddUnifiedWideEvents
- [x] `src/VsaResults.Messaging/WideEvents/IMessageWideEventEmitter.cs` - Remove WideEventEmitterAdapter (depends on legacy types)
- [x] `src/VsaResults.Features/WideEvents/Unified/WideEventBuilder.cs` - Fix string-based NoOp detection (Fix 7)
- [x] `src/VsaResults.Features/Features/FeatureContext.cs` - Encapsulate dictionaries (Fix 5)
- [x] `tests/Integration/FeatureWideEventTests.cs` - Rewrite tests against unified types
- [x] `tests/VsaResult/FeaturePipelineTests.cs` - Update CaptureWideEventEmitter and tests
- [x] `tests/Integration/TestWideEventEmitter.cs` - Rewrite against unified types
- [x] `tests/Integration/MessagingIntegrationTests.cs` - Remove WideEventEmitterAdapter tests
- [x] `tests/WideEventContentDemo.cs` - Rewrite against unified types
- [x] `samples/VsaResults.Sample.WebApi/Program.cs` - Update DI registration
