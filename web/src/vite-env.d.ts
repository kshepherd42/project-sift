/// <reference types="vite/client" />

interface ImportMetaEnv {
	readonly VITE_UPLOAD_MAX_REQUEST_BYTES?: string;
	readonly VITE_UPLOAD_MAX_FILE_BYTES?: string;
	readonly VITE_UPLOAD_MAX_FILE_COUNT?: string;
}
