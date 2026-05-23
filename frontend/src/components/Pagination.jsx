function visiblePages(current, total) {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }
  const pages = new Set([1, total, current, current - 1, current + 1]);
  return [...pages]
    .filter((p) => p >= 1 && p <= total)
    .sort((a, b) => a - b);
}

export default function Pagination({ page, totalPages, onPageChange }) {
  if (totalPages <= 1) return null;

  const pages = visiblePages(page, totalPages);

  return (
    <div className="flex flex-wrap items-center justify-center gap-1.5 py-3">
      {pages.map((p, i) => {
        const prev = pages[i - 1];
        const gap = prev != null && p - prev > 1;
        return (
          <span key={p} className="flex items-center gap-1.5">
            {gap && <span className="px-0.5 text-slate-400">…</span>}
            <button
              type="button"
              onClick={() => onPageChange(p)}
              className={`min-w-[2.25rem] rounded-lg border px-2.5 py-1.5 text-sm font-semibold transition ${
                p === page
                  ? "border-brand-600 bg-brand-600 text-white shadow-sm"
                  : "border-slate-200 bg-white text-slate-700 hover:border-brand-400 hover:text-brand-700"
              }`}
              aria-current={p === page ? "page" : undefined}
            >
              {p}
            </button>
          </span>
        );
      })}
    </div>
  );
}
