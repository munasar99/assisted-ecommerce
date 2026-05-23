export default function AdminModal({ title, children, onClose, onSave, saveLabel = "Save", saving }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4 backdrop-blur-sm">
      <div
        className="w-full max-w-md rounded-2xl border border-slate-200 bg-white shadow-2xl"
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h3 className="text-lg font-bold text-slate-900">{title}</h3>
          <button
            type="button"
            className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
            onClick={onClose}
            aria-label="Close"
          >
            ✕
          </button>
        </div>
        <div className="space-y-4 p-6">{children}</div>
        <div className="flex gap-2 border-t border-slate-100 px-6 py-4">
          <button type="button" className="btn-secondary flex-1" onClick={onClose}>
            Cancel
          </button>
          {onSave && (
            <button type="button" className="btn-primary flex-1" disabled={saving} onClick={onSave}>
              {saving ? "Saving…" : saveLabel}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
