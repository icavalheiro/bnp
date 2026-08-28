<div align="center">
  <img src="src/Bnp.App/Assets/BNP.ico" alt="Better Notepad icon" width="96" height="96">
  <h1>Better Notepad</h1>
  <p>A fast, local-first desktop editor for notes that deserve just a good enough plain text box.</p>

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.1-8B44AC)](https://avaloniaui.net/)
[![Windows and Linux](https://img.shields.io/badge/platform-Windows%20%7C%20Linux-3076C9)](#requirements)
[![AGPL v3](https://img.shields.io/badge/license-AGPL--3.0-663366)](#license)

</div>

> [!NOTE]
> Better Notepad (BNP) is an active prototype. The core editing experience is usable, but packaging and some planned workflows are not finished yet.

BNP combines the immediacy of a native notepad with persistent vertical tabs, rich-text formatting, automatic local saves, and optional Dropbox synchronization. Your documents remain available offline in an application-managed SQLite database, so startup and editing never depend on a network connection.
I tried my best to keep startup times within 200 ms (150ms most of times) since this is my own workflow on how I work with notepads, I just open then when I need to note something down during a call or something, so I need it to be FAST!

## What works today

- Persistent documents and vertical tabs, restored between sessions
- Document creation, selection, renaming, and icon assignment
- Rich-text editing with bold, italic, highlight, alignment, undo, and redo
- Debounced autosave with visible unsaved, saving, saved, and failed states
- Light and dark themes plus English, Portuguese, Spanish, and French localization (becouse that's the only languages I can speak)
- Optional Dropbox synchronization with protected OAuth refresh tokens
- Native AOT, self-contained, single-file publishing for Windows x64 and Linux x64
- Keyboard-accessible core controls and a compact, code-only Avalonia UI (ugh axml 🤮)

## On the roadmap

Search, document export/import, desktop shortcuts, images, tables, trash and recovery, richer conflict handling, and linux packaging are all planned. LLM-assisted editing is a longer-term possibility and will require explicit user control and review before any generated change is applied.

The detailed scope and acceptance criteria live in the [product requirements](docs/product-requirements.md). The [prototype notes](docs/prototype.md) track the current implementation and known gaps.

## Getting started

### Requirements

- [.NET SDK 10.0.300](https://dotnet.microsoft.com/download/dotnet/10.0), as pinned by `global.json`
- Windows x64 or Linux x64
- On Linux, `libsecret-tools` and an active desktop Secret Service are required only for Dropbox authentication

Clone and run the app:

```bash
git clone https://github.com/icavalheiro/bnp.git
cd bnp
dotnet run --project src/Bnp.App/Bnp.App.csproj -c Release
```

Build the full solution:

```bash
dotnet build BNP.slnx -c Release
```

Run the test suite:

```bash
dotnet test --project tests/Bnp.Tests/Bnp.Tests.csproj -c Release
```

Create a Native AOT build for the current operating system:

```bash
dotnet publish src/Bnp.App/Bnp.App.csproj -c Release
```

Published artifacts are self-contained and target the host operating system. Native AOT does not support cross-OS publishing, so Windows and Linux builds must be produced on their matching platforms.

## Dropbox development

BNP can synchronize its SQLite database through Dropbox. Development builds and forks should use their own Dropbox app key:

```powershell
$env:BNP_DROPBOX_CLIENT_ID = "your-app-key"
```

```bash
export BNP_DROPBOX_CLIENT_ID="your-app-key"
```

See [cloud backup setup](docs/cloud-backups.md) for permissions, redirect URI, storage behavior, and credential protection details. Never commit OAuth tokens or client secrets.

## Project structure

```text
src/Bnp.App/       Desktop application, UI, persistence, and services
src/Bnp.Core/      Document models and repository contracts
tests/Bnp.Tests/   Automated persistence, localization, and service tests
tools/             Startup benchmarking tools
docs/              Requirements, implementation notes, and cloud setup
```

The application UI is built entirely in C# with Avalonia; BNP does not author AXAML or XAML. Local data is stored in `BNP/bnp.db` under the operating system's application-data directory.

## Contributing

Contributions are welcome, whether they fix a sharp edge, improve accessibility, expand test coverage, refine documentation, or implement a roadmap item.

1. Check the [open issues](https://github.com/icavalheiro/bnp/issues) or start a discussion before taking on a large change.
2. Fork the repository and create a focused branch.
3. Keep changes small and consistent with the code-only UI rule.
4. Add or update tests for behavioral changes.
5. Run the Release build and test commands above.
6. Open a pull request explaining the problem, the approach, and how you validated it.

Good places to begin include editor round-trip tests, keyboard and screen-reader validation, Fedora runtime testing, localization improvements, and focused items from the roadmap.

## License

Better Notepad is free and open-source software licensed under the [GNU Affero General Public License v3.0](LICENSE). If you modify and provide the software over a network, the AGPL requires you to make the corresponding source code available to its users.

## AI Disclaimer

This projects was developed with assistance from LLM models.
