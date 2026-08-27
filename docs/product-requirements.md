# Better Notepad — Product Requirements

## 1. Objective and scope

### 1.1 Product vision

Better Notepad is a fast, cross-platform desktop editor that combines the immediacy of a native notepad application with persistent vertical tabs, structured rich content, optional cloud synchronization, and file-like documents stored internally by the application.

The primary target is Linux, especially Fedora. Windows must also be supported unless the project is explicitly reduced to a single-platform release.

### 1.2 Product goals

- Open to usable content with minimal perceived delay.
- Save and load documents without interrupting editing.
- Keep documents available between sessions as persistent vertical tabs.
- Let users identify documents visually with icons.
- Synchronize the document library through Google Drive or Dropbox.
- Create desktop shortcuts that open a specific internal document.
- Export documents into a portable, shareable format.
- Support rich text and structured visual content without losing a simple writing experience.
- Provide an optional LLM-assisted editing workflow under explicit user control.
- Offer a polished interface that follows platform conventions where practical and otherwise uses a restrained GNOME-inspired visual language.

### 1.3 MVP scope

The proposed MVP includes:

- Linux (Fedora) and Windows desktop applications.
- Local document creation, editing, renaming, deletion, and restoration after restart.
- A persistent vertical tab list on the left.
- Plain text and a defined subset of rich-text formatting.
- Per-document icons from a built-in icon set.
- Search by document title and textual content.
- Local persistence in a single application-managed SQLite database.
- Automatic local saving and recovery after an abnormal shutdown.
- Export and import using a documented portable format.
- Creation of desktop shortcuts for individual internal documents.
- Keyboard-accessible core workflows.

Cloud synchronization and LLM integration are treated as post-MVP capabilities unless they are promoted after the open questions are resolved. Their contracts and expected behavior are still specified below so the local data model does not block them.

### 1.4 Post-MVP scope

- Google Drive synchronization.
- Dropbox synchronization.
- LLM-assisted document editing.
- User-supplied document icons.
- Advanced tables or spreadsheet-like calculations.
- Additional export formats such as Markdown, HTML, PDF, or DOCX.
- Multiple windows and detachable tabs.
- Mobile or web clients.
- Real-time multi-user collaboration.

## 2. Stakeholders and actors

| Actor             | Description                                         | Primary needs                                                       |
| ----------------- | --------------------------------------------------- | ------------------------------------------------------------------- |
| Local user        | Person creating and editing documents on one device | Immediate startup, reliable saving, clear navigation                |
| Multi-device user | Person using the same library on multiple computers | Predictable synchronization and conflict handling                   |
| Recipient         | Person receiving an exported document               | Portable, documented content with preserved formatting              |
| Cloud provider    | Google Drive or Dropbox API                         | Authorized, rate-limited, secure integration                        |
| LLM provider      | Local or remote language model service              | Explicitly selected content and controlled credentials              |
| Operating system  | Fedora/Linux or Windows desktop environment         | Shortcut registration, file associations, secure credential storage |

## 3. User stories

### US-01 — Create and edit a document

As a local user, I want to create a document and start typing immediately so that the application feels as direct as a basic notepad.

### US-02 — Navigate using vertical tabs

As a local user, I want my open or pinned documents listed vertically on the left so that many documents remain identifiable without shrinking horizontal tabs.

### US-03 — Identify a document with an icon

As a local user, I want to assign an icon to a document so that I can recognize its purpose quickly.

### US-04 — Preserve work automatically

As a local user, I want edits saved automatically and restored after restart or failure so that I do not lose work.

### US-05 — Format document content

As a local user, I want rich text, highlights, alignment, grids, and images so that documents can represent more than plain notes.

### US-06 — Search the library

As a local user, I want to search document titles and contents so that internally stored documents remain discoverable.

### US-07 — Open a document from the desktop

As a local user, I want a desktop shortcut for a specific internal document so that it behaves like a familiar file even though its source is in the application database.

### US-08 — Export and import a document

As a local user, I want to export an internal document into a shareable package and import it later so that I can exchange or back up individual documents.

### US-09 — Synchronize through a cloud provider

As a multi-device user, I want to synchronize my library through Google Drive or Dropbox so that documents are available across supported devices.

### US-10 — Resolve synchronization conflicts

As a multi-device user, I want concurrent changes preserved and clearly presented so that synchronization never silently discards content.

### US-11 — Use LLM-assisted editing

As a local user, I want to ask an LLM to transform selected content and preview the result before applying it so that AI cannot modify my document without review.

### US-12 — Use the application without a mouse

As a keyboard user, I want to create, select, format, search, and save documents from the keyboard so that core workflows are efficient and accessible.

## 4. Acceptance criteria

### AC-01 — Startup and first content render

- Given an existing local library, when the user launches the application, then the application displays an interactive editor and the most recently active document without waiting for cloud synchronization.
- Startup does not require network access or authentication.
- Cloud synchronization and nonessential indexing run after the first usable render.
- Performance is measured using defined reference hardware and a representative library; targets are listed under non-functional requirements.

### AC-02 — Vertical tabs

- The tab list is positioned on the left in the default layout.
- Each entry can display an icon, title, dirty/saving state, and conflict state.
- The user can create, select, rename, reorder, pin, and close or hide entries using mouse and keyboard controls.
- Closing or hiding a tab does not delete the underlying document.
- The selected tab and scroll position are restored after restarting the application.

### AC-03 — Icons

- A document can be assigned one icon from the supported icon catalogue.
- The chosen icon persists across restarts, export/import, and synchronization.
- Removing an icon returns the document to a default document icon.
- Unsupported imported icons fall back safely without blocking document import.

### AC-04 — Local persistence and recovery

- A newly created document receives a stable unique identifier independent of its title.
- Edits are persisted automatically according to a documented debounce or transaction policy.
- The UI distinguishes unsaved, saving, saved, and failed states.
- A failed save does not clear the in-memory edit and presents a retryable error.
- After an abnormal shutdown, the application restores all edits confirmed as persisted before the interruption.
- Database writes are transactional and cannot leave a partially written document revision.

### AC-05 — Rich content

- The editor supports, at minimum, paragraphs, headings, bold, italic, underline, strikethrough, text highlights, left/center/right alignment, lists, links, images, and tables or grids.
- Undo and redo treat formatting and content changes as reversible editing operations.
- Pasting unsupported rich content degrades predictably and does not crash the application.
- Images remain associated with the document after restart, export/import, and synchronization.
- The exact MVP table/grid feature set is resolved before implementation.

### AC-06 — Desktop shortcuts

- A user can create a shortcut for a selected document from within the application.
- Activating the shortcut launches the application if needed and focuses the document identified by its stable ID.
- Renaming a document does not invalidate its shortcut.
- If the document no longer exists, the application opens and shows a clear, recoverable error.
- Linux shortcut generation follows the desktop entry specification; Windows shortcut generation uses a supported shell-link mechanism.
- Shortcut names and icons are sanitized and do not allow arbitrary command injection.

### AC-07 — Export and import

- Export produces a versioned, documented package containing document metadata, structured content, and referenced image assets.
- Import validates format version, schema, size limits, identifiers, and asset types before persistence.
- Importing a malformed or unsupported package does not alter the existing library.
- Import handles identifier collisions according to a documented rule and never silently overwrites a local document.
- Exported packages contain no cloud credentials, local file paths, LLM credentials, or unrelated application data.

### AC-08 — Cloud synchronization

- The user can connect and disconnect each supported provider explicitly.
- The application remains fully usable while offline.
- Local changes queue for later synchronization without blocking save operations.
- Sync status is available per document and for the whole account.
- Provider, authentication, network, rate-limit, and storage-quota failures are distinguishable and retryable where appropriate.
- Concurrent edits never cause silent last-writer-wins data loss.
- Disconnecting an account does not delete the local library unless the user separately confirms a destructive action.

### AC-09 — LLM-assisted editing

- LLM actions operate only on content explicitly selected or included by the user.
- The user can review a diff or preview before accepting a change.
- Canceling or rejecting the result leaves the document unchanged.
- Applying a result creates one undoable editor operation.
- Network transmission, provider identity, and the content being shared are disclosed before the first remote request.
- API keys are stored in the operating system credential store and are never written to the document database or logs.

### AC-10 — Accessibility and platform behavior

- All core actions are keyboard reachable with visible focus indicators.
- Controls expose accessible names, roles, states, and relationships.
- Text and essential controls meet WCAG 2.2 AA contrast requirements.
- The interface supports platform text scaling without clipping or overlapping content.
- Platform-standard shortcuts are used where conventions differ between Linux and Windows.

### AC-11 — Application layout and visual identity

- The default window has a compact application menu or command bar, a left vertical document area, a primary editor, and a status bar.
- The editor remains the dominant surface and is not enclosed in a decorative card.
- The left document area can collapse to preserve editor width and exposes an accessible control to restore it.
- Frequently used formatting commands are available near the editor; secondary commands remain discoverable without permanently crowding the toolbar.
- Icon-only commands use recognizable platform or toolkit icons and expose tooltips and accessible names.
- Save, sync, and conflict indicators do not resize tabs or shift the editor layout when their state changes.
- On Fedora, controls, typography, spacing, focus, menus, and dialogs follow GNOME conventions where the selected toolkit permits it.
- On Windows, window behavior, menus, keyboard conventions, system themes, and accessibility integrate with current Windows expectations where practical.
- Light, dark, and high-contrast system preferences are respected; the application does not require a restart to apply a supported theme change.
- Native controls are preferred when they satisfy cross-platform behavior, performance, accessibility, and rich-editing requirements. Any custom control must provide equivalent keyboard and assistive-technology behavior.
- The layout remains usable at the agreed minimum window size and at 200% text scaling without overlapping controls or hiding the active document state.

## 5. Business rules

1. A document is an application-owned entity, not necessarily an operating-system file.
2. Every document has an immutable unique ID and mutable title, icon, content, timestamps, and state metadata.
3. Document titles do not need to be globally unique unless later required for export or provider compatibility.
4. A vertical tab represents a view onto a document. Closing a tab and deleting a document are separate actions.
5. Local persistence is the source used for immediate startup and editing; cloud providers are synchronization targets, not runtime dependencies.
6. Local save completion must not wait for cloud upload completion.
7. Automatic saving must not create a separate undo operation visible to the user.
8. Synchronization must preserve both versions of an unresolved conflict.
9. LLM-generated changes are proposals until explicitly accepted by the user.
10. Desktop shortcuts reference immutable document IDs rather than titles or database row numbers.
11. Export formats are versioned and backward compatibility expectations must be documented per version.
12. External images or files are copied into application-managed storage or embedded according to the final asset-storage decision; they must not depend silently on their original path.
13. Deletion behavior, retention, and trash recovery must be decided before release.

## 6. Main flow and exception scenarios

### 6.1 Main editing flow

1. The user starts the application.
2. The shell and editor render using local state.
3. The most recently active document is selected.
4. The user creates or selects a document from the vertical tab list.
5. The user edits content and formatting.
6. Changes are saved locally in the background.
7. If cloud sync is enabled, persisted changes are queued for upload.
8. The UI reports local save and cloud sync as separate states.

### 6.2 Create and use a desktop shortcut

1. The user selects a document and chooses “Create desktop shortcut.”
2. The application requests the shortcut name and optional icon where required.
3. The application creates a platform-specific shortcut containing an application deep link or launch argument with the document ID.
4. The user activates the shortcut.
5. The application opens or receives the activation and focuses the matching document.

Exceptions:

- Shortcut creation permission is denied: no partial shortcut remains and remediation is shown.
- The document was deleted: the app offers to dismiss the error or open the library; restoration is offered only if a trash feature exists.
- The application moved or was uninstalled: behavior depends on packaging and installer registration.

### 6.3 Export and import

1. The user chooses a document and requests export.
2. The application validates that all referenced assets are locally available.
3. The application creates a portable package at a user-selected location.
4. A recipient selects the package for import or opens it through file association.
5. The application validates the package in an isolated staging step.
6. The user previews metadata and confirms import.
7. The document and assets are committed in one logical transaction.

Exceptions:

- Missing or corrupt asset: import fails safely or offers a clearly marked partial import, depending on the unresolved policy.
- Unsupported newer format version: the package remains unchanged and the user is informed.
- Identifier collision: import creates a new ID or offers version replacement, depending on the unresolved policy.

### 6.4 Cloud synchronization

1. The user authorizes a provider through its supported OAuth flow.
2. The application stores tokens in the operating system credential store.
3. The sync engine compares local and remote manifests or revisions.
4. Nonconflicting changes transfer in the background.
5. Conflicts create preserved variants and a visible resolution task.
6. The application updates local sync metadata only after remote confirmation.

Exceptions:

- Offline: changes remain queued locally.
- Authentication expired: editing continues and reauthentication is requested.
- Provider quota exceeded: local save continues and sync displays a blocking provider status.
- Remote data is malformed: it is quarantined and does not replace valid local data.
- Simultaneous edits: both versions are retained until automatically or manually merged.

### 6.5 LLM-assisted editing

1. The user selects content and invokes an LLM action.
2. The application shows or collects the instruction and identifies the configured provider.
3. The user submits the request.
4. The request runs asynchronously without locking unrelated editing.
5. The result appears as a preview or diff.
6. The user accepts, modifies, retries, or rejects the proposal.
7. An accepted proposal is recorded as an undoable document change.

Exceptions:

- No provider is configured: configuration is offered without losing the selection.
- The request fails or times out: the document is unchanged and retry is available.
- The document changes while a request is running: the result is marked stale and is not applied automatically.
- The response exceeds limits or contains unsupported structure: a safe preview is shown or the result is rejected with an explanation.

## 7. Data and validation requirements

### 7.1 Conceptual data model

| Entity            | Required data                                                                                     | Notes                                         |
| ----------------- | ------------------------------------------------------------------------------------------------- | --------------------------------------------- |
| Document          | ID, title, content format version, structured content, created/updated timestamps, deletion state | Primary user-owned entity                     |
| Document asset    | ID, document ID, media type, byte length, checksum, storage reference                             | Images and future attachments                 |
| Tab state         | document ID, order, pinned/visible state, selected state                                          | Presentation state, not document identity     |
| Icon              | icon type, icon key or asset ID                                                                   | Built-in initially; custom later              |
| Revision          | revision ID, document ID, parent revision, timestamp, device ID, content checksum                 | Supports recovery and sync conflict detection |
| Sync account      | provider, account identifier, connection state                                                    | Tokens must not be stored here                |
| Sync state        | document ID, provider, remote ID, remote revision, local revision, status, last attempt           | One document may have provider-specific state |
| App state         | schema version, last active document, window/layout state                                         | Must be cheap to load at startup              |
| LLM configuration | provider type, model preference, nonsecret settings                                               | Secrets remain in OS credential storage       |

### 7.2 Content representation

The canonical rich-content representation must be structured and versioned. It must support deterministic serialization, schema validation, unknown-node handling, and migration. YAML may be human-readable metadata, but a plain YAML file is not sufficient by itself for binary images and can be unsafe if imported with permissive object deserialization.

A candidate portable package is a ZIP-compatible container with:

- `manifest.yml` or `manifest.json` for versioned metadata;
- a structured content document;
- an `assets/` directory containing images addressed by stable ID or checksum.

The exact canonical editor model and interchange schema remain open decisions.

### 7.3 Validation

- Titles must have a documented maximum length and must reject or normalize prohibited control characters.
- Rich-content depth, node count, image dimensions, asset size, and total import size must be bounded.
- Imported paths must be relative, normalized, and protected against path traversal.
- Media types must be allowlisted and verified from content, not trusted from file extensions alone.
- Checksums must detect corrupt or incomplete assets and sync payloads.
- SQLite schema migrations must be versioned, transactional, and recoverable from backup.
- Cloud payloads and LLM responses are untrusted external input and must be validated before display or persistence.
- User-authored text must never be interpreted as executable markup or commands without sanitization.

## 8. Non-functional requirements

### 8.1 Performance

Final thresholds require reference hardware, cold/warm definitions, and a representative data set. Proposed release targets are:

| Metric                                                  | Proposed target                             |
| ------------------------------------------------------- | ------------------------------------------- |
| Warm launch to interactive editor                       | p95 ≤ 250 ms                                |
| Cold launch to interactive editor                       | p95 ≤ 800 ms                                |
| First local document content visible after shell render | p95 ≤ 100 ms                                |
| Local save acknowledgement for a typical edit           | p95 ≤ 50 ms after save debounce begins      |
| Switch between already loaded documents                 | p95 ≤ 50 ms                                 |
| Switch to a nonloaded text-centric document             | p95 ≤ 150 ms                                |
| Search feedback after typing                            | begins within 100 ms without blocking input |
| Typing responsiveness                                   | no visible input stalls during save or sync |

A “typical” document and maximum supported document size must be defined. Startup measurements must exclude cloud completion and include production builds with telemetry disabled or controlled consistently.

### 8.2 Reliability and data integrity

- Local editing works without network access.
- Database transactions, write-ahead logging, checkpoints, and backups are configured and tested against abrupt termination.
- The application detects migration failure and preserves the prior database for recovery.
- Export/import and sync operations are idempotent where practical.
- Fault injection tests cover process termination during save, import, migration, and sync metadata updates.

### 8.3 Security and privacy

- OAuth uses provider-supported authorization flows with least-privilege scopes.
- Tokens and API keys use platform credential storage: Secret Service-compatible storage on Linux and Windows Credential Manager or an equivalent supported API on Windows.
- Secrets, document content, and remote payloads are excluded from logs by default.
- LLM integration is opt-in and clearly distinguishes local from remote providers.
- Remote LLM requests send the minimum necessary content and are never initiated automatically.
- Import processing defends against decompression bombs, path traversal, oversized content, malformed images, and unsafe serialization.
- Desktop shortcut values and deep-link inputs are treated as untrusted and validated.

### 8.4 Accessibility and usability

- Meet WCAG 2.2 AA where applicable to desktop software.
- Support keyboard navigation, screen readers, high-contrast themes, and text scaling.
- Respect reduced-motion settings.
- Preserve familiar platform behaviors while maintaining a consistent information architecture.
- Destructive actions require clear confirmation or a recoverable trash mechanism.

### 8.5 Compatibility and packaging

- Fedora support must name the minimum supported Fedora release before beta.
- Windows support must name the minimum supported Windows release before beta.
- Packaging candidates include Flatpak/RPM for Fedora and MSIX or a conventional installer for Windows.
- File associations, desktop shortcuts, credential storage, and update behavior must be tested in each selected package format.
- The core document format and database schema must not depend on platform-specific UI types.

### 8.6 Observability

- Local diagnostic logs use structured events and avoid document content and secrets.
- Users can locate and export diagnostic logs explicitly.
- Startup, save, load, sync, migration, and import failures have actionable error categories.
- Product analytics are not required for the MVP; if introduced, they require explicit scope, privacy review, and user controls.

## 9. Dependencies, risks, and impact

### 9.1 Technology options

No technology stack is selected by this document.

| Option                       | Advantages                                                                                                                            | Risks and trade-offs                                                                                                                                                                                        |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| C# + Avalonia                | Shared Linux/Windows UI, mature .NET tooling, fast development, SQLite and async ecosystem, custom styling with accessibility support | Not fully native controls on every platform; startup, trimming, packaging, text editing, and rich-content behavior require prototypes and measurement                                                       |
| C++ + native platform UIs    | Maximum platform integration and performance control                                                                                  | Maintaining separate Linux and Windows UI layers is costly; Linux toolkit choice affects GNOME fit; rich editor, accessibility, packaging, and cloud SDK integration carry higher implementation complexity |
| C++ + cross-platform toolkit | Native compilation and shared code depending on toolkit                                                                               | Visual identity, licensing, accessibility, startup, rich text capability, and distro packaging vary significantly by toolkit                                                                                |

A short technical spike should compare production-like prototypes rather than “hello world” startup. Each prototype should render the vertical document list and a representative rich document, load SQLite data, perform a save, expose keyboard navigation, and produce installable Fedora and Windows packages.

### 9.2 Key dependencies

- Cross-platform desktop UI framework or platform toolkits.
- Rich-text editing engine with tables, images, undo/redo, accessibility, and serialization support.
- SQLite and migration tooling.
- Google Drive and Dropbox OAuth and storage APIs.
- Platform credential stores.
- Linux desktop entry and Windows shell-link integration.
- A stable structured-content schema and portable package format.
- Optional LLM provider APIs or a local inference interface.

### 9.3 Key risks

| Risk                                                   | Impact                                          | Mitigation or decision needed                                                             |
| ------------------------------------------------------ | ----------------------------------------------- | ----------------------------------------------------------------------------------------- |
| “Virtually zero” startup is undefined                  | Performance cannot be accepted objectively      | Agree reference hardware, scenarios, percentiles, and targets                             |
| Rich-text editor capability differs by framework       | Core features may require expensive custom work | Prototype formatting, tables, images, IME, accessibility, and serialization first         |
| Single SQLite library and cloud sync can diverge       | Data loss or difficult conflicts                | Use immutable IDs, revisions, checksums, explicit conflict states, and fault tests        |
| Binary assets increase database and sync cost          | Slow backup, startup, and database growth       | Benchmark BLOB storage versus managed asset files before deciding                         |
| Native look conflicts with identical cross-platform UI | Design inconsistency or duplicated UI work      | Define which platform behaviors are adaptive and which brand elements remain shared       |
| Desktop shortcut behavior varies by packaging          | Feature may fail in sandboxed installs          | Test early with selected Flatpak/RPM/MSIX distribution models                             |
| Remote LLM use can expose private text                 | Privacy and compliance risk                     | Opt-in provider configuration, explicit send/review flow, minimal context, secure secrets |
| YAML import can enable unsafe parsing                  | Code execution or resource exhaustion           | Use safe schema-bound parsing, strict limits, and a package staging area                  |

### 9.4 Expected architectural boundaries

The implementation should keep these responsibilities separable regardless of language or UI toolkit:

- Presentation and editor interaction.
- Canonical document model and serialization.
- Local persistence and migrations.
- Search and indexing.
- Sync engine and provider adapters.
- Import/export and package validation.
- Platform integration for shortcuts, credentials, and file associations.
- Optional LLM orchestration and provider adapters.

This separation is a requirement for testability and platform support, not a mandate for separate processes or services.

## 10. Open questions, assumptions, and out-of-scope items

### 10.1 Open questions requiring product decisions

1. Is Windows required for the first public release, or may the first release target Fedora only?
2. Which Fedora and Windows versions must be supported?
3. Does a vertical tab represent every document in the library, only currently open documents, pinned documents, or a configurable combination?
4. Can users create folders, tags, workspaces, or collections, or is the tab list flat?
5. What does “grids” mean: simple rich-text tables, spreadsheet-like cells, freeform layout grids, or something else?
6. Which formatting features are mandatory for MVP versus later releases?
7. Should images be embedded in SQLite, stored in an application-managed asset directory, or selected through benchmark results?
8. Is the canonical document format intended to be human-editable outside the app, or only portable and documented?
9. Should export be one YAML file with encoded assets, a ZIP package with YAML/JSON metadata, or multiple selectable formats?
10. How should import handle an existing document with the same ID: duplicate it, replace it, merge revisions, or ask each time?
11. Should deleted documents move to a recoverable trash, and for how long?
12. Does cloud sync cover the full library as one application database/package, individual documents, or both?
13. May the same library connect to Google Drive and Dropbox simultaneously?
14. What conflict-resolution experience is acceptable: keep both copies, field-level merge, rich-text visual diff, or a combination?
15. Must cloud content use end-to-end encryption controlled by the user, beyond provider-side encryption?
16. Is LLM support bring-your-own-key, bundled with a service, local-model only, or a combination?
17. Which LLM actions are desired initially: rewrite, summarize, translate, change tone, generate, or freeform instructions?
18. Are plugins or third-party extensions a future requirement?
19. What are the reference hardware and representative library/document sizes for performance acceptance?
20. Is automatic application updating required, and through which distribution channels?

### 10.2 Recorded assumptions

- The application is single-user on each operating-system account.
- Local editing remains fully functional without cloud or LLM configuration.
- Documents are private by default and are never uploaded without explicit provider configuration.
- The initial icon catalogue can use redistributable built-in icons; arbitrary user icons are not required for MVP.
- A simple rich-text table satisfies the word “grid” only as a temporary planning assumption and must be confirmed.
- The application will use a local database, but SQLite versus an alternative remains subject to an implementation spike if performance or sync requirements disprove suitability.

### 10.3 Out of scope unless promoted

- Real-time collaboration and shared cursors.
- Browser and mobile applications.
- Server-hosted user accounts owned by this product.
- Spreadsheet formulas and database-style tables.
- Plugin execution and macro scripting.
- Full Microsoft Word document compatibility.
- Automatic, unsupervised LLM modification of documents.
- Synchronizing arbitrary files outside the application library.
