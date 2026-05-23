import { IconDelete, IconEdit, IconView } from "./AdminIcons";

export default function AdminRowActions({ onView, onEdit, onDelete, compact }) {
  const btn =
    "inline-flex items-center justify-center gap-1.5 rounded-lg px-2.5 py-1.5 text-xs font-semibold transition";
  return (
    <div className={`flex flex-wrap items-center gap-1.5 ${compact ? "" : "min-w-[7.5rem]"}`}>
      {onView && (
        <button
          type="button"
          title="View"
          className={`${btn} border border-slate-200 bg-white text-slate-700 hover:bg-slate-50`}
          onClick={onView}
        >
          <IconView />
          {!compact && "View"}
        </button>
      )}
      {onEdit && (
        <button
          type="button"
          title="Edit (PUT)"
          className={`${btn} border border-brand-200 bg-brand-50 text-brand-800 hover:bg-brand-100`}
          onClick={onEdit}
        >
          <IconEdit />
          {!compact && "Edit"}
        </button>
      )}
      {onDelete && (
        <button
          type="button"
          title="Delete"
          className={`${btn} border border-red-200 bg-red-50 text-red-700 hover:bg-red-100`}
          onClick={onDelete}
        >
          <IconDelete />
          {!compact && "Delete"}
        </button>
      )}
    </div>
  );
}
