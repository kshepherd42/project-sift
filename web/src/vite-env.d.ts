/// <reference types="vite/client" />

interface ImportMetaEnv {
	readonly VITE_UPLOAD_MAX_REQUEST_BYTES?: string;
	readonly VITE_UPLOAD_MAX_FILE_BYTES?: string;
	readonly VITE_UPLOAD_MAX_FILE_COUNT?: string;
	readonly VITE_BATCH_POLL_INTERVAL_MS?: string;
	readonly VITE_BATCH_REQUEST_TIMEOUT_MS?: string;
	readonly VITE_BATCH_LONG_RUNNING_THRESHOLD_MS?: string;
}
