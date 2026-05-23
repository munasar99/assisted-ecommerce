import { useRef, useState } from "react";
import { validateImageFile } from "../utils/validation";

export default function UploadInput({ label, onChange, error }) {
  const inputRef = useRef(null);
  const [preview, setPreview] = useState(null);

  const clearPreview = () => {
    if (preview) URL.revokeObjectURL(preview);
    setPreview(null);
    if (inputRef.current) inputRef.current.value = "";
    onChange(null, null);
  };

  const handleFile = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const err = validateImageFile(file);
    if (err) {
      onChange(null, err);
      return;
    }
    if (preview) URL.revokeObjectURL(preview);
    setPreview(URL.createObjectURL(file));
    onChange(file, null);
  };

  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-700">{label}</label>
      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        onChange={handleFile}
        className="block w-full text-sm text-slate-600 file:mr-4 file:rounded-lg file:border-0 file:bg-brand-50 file:px-4 file:py-2 file:text-brand-700"
      />
      {preview && (
        <div className="relative mt-3 inline-block">
          <img
            src={preview}
            alt="Preview"
            className="h-32 w-32 rounded-lg border border-slate-200 object-cover shadow-sm"
          />
          <button
            type="button"
            onClick={clearPreview}
            className="absolute -right-2 -top-2 flex h-7 w-7 items-center justify-center rounded-full border border-slate-200 bg-white text-sm font-bold text-slate-600 shadow-md transition hover:bg-red-50 hover:text-red-600"
            aria-label="Ka saar sawirka"
            title="Ka saar sawirka"
          >
            ×
          </button>
        </div>
      )}
      {error && <p className="mt-1 text-sm text-red-600">{error}</p>}
    </div>
  );
}
