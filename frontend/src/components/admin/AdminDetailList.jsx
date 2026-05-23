export default function AdminDetailList({ rows }) {
  return (
    <dl className="space-y-2 text-sm">
      {rows.map(({ label, value }) => (
        <div key={label} className="flex justify-between gap-4 border-b border-slate-50 py-1.5">
          <dt className="shrink-0 text-slate-500">{label}</dt>
          <dd className="text-right font-medium text-slate-900">{value ?? "—"}</dd>
        </div>
      ))}
    </dl>
  );
}
