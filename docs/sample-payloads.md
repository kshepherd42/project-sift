# ProjectSift API sample payloads

Sample payload files are now committed at the repository root in:

- `sample payloads/`

Use those files directly for multipart uploads instead of generating them from this document.

## API endpoint

- `POST http://localhost:5050/v1/batches`

## Quick upload examples (PowerShell)

```powershell
curl.exe -X POST "http://localhost:5050/v1/batches" -F "files=@.\sample payloads\one-document.bin"
curl.exe -X POST "http://localhost:5050/v1/batches" -F "files=@.\sample payloads\first.bin" -F "files=@.\sample payloads\second.bin"
curl.exe -X POST "http://localhost:5050/v1/batches" -F "files=@.\sample payloads\malformed.bin"
```

For scenario mapping and expected responses, see `sample payloads/README.md`.
