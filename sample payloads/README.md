# Sample payload files

This folder contains upload-ready payload files for `POST /v1/batches`.

## Files and scenarios

- `one-document.bin`: one valid JSON object.
- `first.bin`: valid JSON object (multi-file scenario, part 1).
- `second.bin`: valid JSON array (multi-file scenario, part 2).
- `nested.bin`: valid deeply nested JSON object.
- `mixed.bin`: one complete JSON object followed by malformed candidate.
- `malformed.bin`: malformed JSON candidate.
- `empty.bin`: empty file (expected 400 invalid upload).
- `oversized.bin`: file over default max file size (expected 400 invalid upload).

## Quick usage (PowerShell)

```powershell
curl.exe -X POST "http://localhost:5050/v1/batches" -F "files=@.\sample payloads\one-document.bin"
curl.exe -X POST "http://localhost:5050/v1/batches" -F "files=@.\sample payloads\first.bin" -F "files=@.\sample payloads\second.bin"
```
