# Cloud backups

BNP synchronizes a SQLite database named `bnp.db` through Dropbox. Before every upload, it downloads the remote database and merges newer records into the local database. Documents use their `updated_at` value; active document, sidebar, theme, and language each have an independent timestamp. The merged database then replaces the remote object.

The remote folder is always `/bnp/` and is created automatically. Synchronization runs 30 seconds after a local database change and no more than once every five minutes.

OAuth refresh tokens are protected with Windows DPAPI on Windows and the desktop Secret Service on Linux. The JSON configuration contains only the provider and Windows-encrypted payload; Linux tokens never enter that file. The SQLite snapshot is created with the SQLite backup API, uploaded from a temporary directory, and then deleted.

On Linux, install `secret-tool` (usually provided by `libsecret-tools`) and run BNP in a desktop session with an active Secret Service such as GNOME Keyring or KDE Wallet. The configuration file is stored under the user's local application-data directory with mode `0600`.

## Dropbox app setup

The official Dropbox App Key `86e6im2xh5nhqqj` is compiled into BNP. In the Dropbox App Console for this app:

1. Select **Full Dropbox** access.
2. Enable `files.metadata.read`, `files.metadata.write`, `files.content.read`, and `files.content.write`.
3. Register the exact OAuth redirect URI `http://127.0.0.1:53682/oauth/callback/`.

Forks and development builds may override the compiled App Key with `BNP_DROPBOX_CLIENT_ID`:

```powershell
$env:BNP_DROPBOX_CLIENT_ID = "another-app-key"
```

```bash
export BNP_DROPBOX_CLIENT_ID="another-app-key"
```

Full Dropbox access is required because `/bnp/` is rooted at the Dropbox account root. Restrict access further only if the product changes the path semantics to Dropbox's app-folder root.

Do not store refresh tokens or client secrets in the repository.
