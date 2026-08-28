# BNP Avalonia Prototype

## Status

The first local vertical slice is implemented with .NET 10 and Avalonia 12.1.1. The application UI is authored entirely in C#. The repository contains no AXAML or XAML files.

Implemented:

- Code-only Avalonia application and window lifecycle.
- Compact GNOME-inspired shell with a collapsible vertical document sidebar.
- Persistent documents, active document, icons, tab order, and sidebar state in SQLite.
- Document creation, selection, renaming, and icon assignment.
- Rich text editing through `AvaloniaRichEditor` with bold, italic, highlight, alignment, undo, and redo.
- Debounced autosave with visible unsaved, saving, saved, and failed states.
- Native AOT publication for Windows x64.
- Automated SQLite migration and restart tests.
- Opt-in process-to-render startup benchmark.
- Dropbox synchronization with timestamp-based SQLite merge.

Not implemented in this slice:

- LLM integration.
- Desktop document shortcuts.
- Portable document export/import.
- Images, tables, search, trash, and conflict handling.
- Fedora packaging and runtime validation.

## Technology

| Component   | Selection                              | Version               |
| ----------- | -------------------------------------- | --------------------- |
| Runtime     | .NET                                   | 10.0.300 SDK baseline |
| UI          | Avalonia Desktop                       | 12.1.1                |
| Base theme  | Avalonia Fluent Theme, compact density | 12.1.1                |
| Rich editor | AvaloniaRichEditor                     | 1.1.0                 |
| Icons       | Lucide.Avalonia                        | 0.2.19                |
| Persistence | Microsoft.Data.Sqlite                  | 10.0.11               |
| Tests       | xUnit v3 on Microsoft.Testing.Platform | 4.0.0                 |

`AvaloniaRichEditor` and `Lucide.Avalonia` are MIT-licensed. The editor remains behind the application's editing boundary in concept, but this first slice integrates its public API directly in `MainWindow`; extracting a formal adapter is the next step before introducing another editor implementation.

## Code-only UI rule

The BNP project must not author AXAML/XAML for application UI, resources, styles, templates, or windows. MSBuild project and props files remain XML because they are build configuration rather than UI.

The current shell, controls, event wiring, theme registration, adaptive GNOME-inspired palette, accessibility names, tooltips, icon controls, and rich-text toolbar are all constructed in C#.

Third-party packages may contain their own compiled resources. BNP does not dynamically load XAML.

## Local data

The database is stored at the platform's local application-data directory under `BNP/bnp.db`.

Schema version 1 contains:

- `documents`: immutable GUID, title, icon key, content format, content, tab order, and timestamps.
- `workspace_state`: active document and collapsed sidebar state.

The repository uses one application-owned connection with WAL, normal synchronous mode, a 2.5-second busy timeout, parameterized SQL, and transactional migrations. Connection pooling is disabled because the repository already owns a persistent connection and must release the database file immediately on disposal.

Content begins as `plain-text-v1` for the initial document and is upgraded to `avalonia-rich-editor-json-v1` on the first rich save.

## Security decisions

- SQL values are parameterized.
- Unknown icon keys render the default file icon.
- Remote and local-file image ingestion is disabled in the rich editor.
- Images and tables are disabled for this slice.
- Autosave failures retain the pending in-memory snapshot and display a failed status.
- No document text, credentials, or secrets are logged.

## Commands

Build:

```powershell
dotnet build BNP.slnx -c Release
```

Run:

```powershell
dotnet run --project src/Bnp.App/Bnp.App.csproj -c Release
```

Test with the .NET 10 Microsoft.Testing.Platform runner:

```powershell
dotnet test --project tests/Bnp.Tests/Bnp.Tests.csproj -c Release
```

Publish Native AOT for the current operating system:

```powershell
dotnet publish src/Bnp.App/Bnp.App.csproj -c Release
```

The project enables Native AOT, self-contained deployment, and single-file output by default. It infers the native RID from the build host: `win-x64` on Windows x64 and `linux-x64` on Linux x64. Native AOT does not support cross-OS compilation, so each artifact must be published on its matching operating system or CI runner.

Benchmark a published executable with 30 process launches:

```powershell
dotnet run --project tools/Bnp.StartupBench/Bnp.StartupBench.csproj `
  -c Release -- src/Bnp.App/bin/Release/net10.0/win-x64/publish/BNP.exe 30
```

The benchmark uses a unique named pipe per launch. The application emits readiness after the window opens and a render-priority callback runs, with the active document already loaded into the editable control. Instrumentation performs no pipe or file I/O during normal launches.

## Validation results

Validated on Windows x64:

- Release solution build: passes with zero warnings and errors.
- SQLite tests: 2 passed, 0 failed.
- Runtime launch: code-only window, SQLite database, and rich editor initialize without an exception.
- Native AOT publish: passes with zero warnings and errors.
- Authored AXAML/XAML files: zero.

Startup measurements from the Native AOT Windows executable, 30 warm-cache process launches:

| Metric       | Initial full editor host | Compact MVP toolbar | Polished adaptive UI |
| ------------ | -----------------------: | ------------------: | -------------------: |
| External p50 |                161.17 ms |           158.32 ms |            158.05 ms |
| External p95 |                207.31 ms |           200.64 ms |            188.35 ms |
| Internal p50 |                148.19 ms |           141.61 ms |            143.78 ms |
| Internal p95 |                175.32 ms |           152.46 ms |            160.98 ms |

The current polished UI meets the p95 at or below 200 ms target for the measured Windows warm-cache scenario, with an external p95 of 188.35 ms. Its database-ready p95 was 86.43 ms and window-constructed p95 was 125.18 ms. A separate 10-run diagnostic contained a cold/outlier launch and showed the pre-database Avalonia bootstrap dominating the worst case; database-to-window and window-to-render work were materially smaller.

The next performance spike should validate the target across the remaining scenarios and compare:

1. Avalonia platform/bootstrap and theme initialization cost on reference Windows and Fedora systems.
2. A minimal `TextBox` editor build against the rich editor build using the same ready definition.
3. Ready-to-run framework-dependent, self-contained Native AOT, and trimmed deployment modes.
4. Fedora native launch behavior and font initialization.

The product must not claim the startup target until every defined scenario passes with at least 30 controlled samples on reference hardware.

## Next implementation steps

1. Add headless editor round-trip tests for formatting and JSON serialization.
2. Extract `IRichDocumentEditor` so the editor dependency can be replaced or benchmarked independently.
3. Add reorder and close/hide behavior for vertical tabs.
4. Validate keyboard navigation, IME, clipboard, text scaling, and screen-reader names on Fedora and Windows.
5. Publish and benchmark `linux-x64` Native AOT on Fedora.
