import { useAuth } from "../context/AuthContext";

export default function AdminHeader({ title, subtitle }) {
  const { admin } = useAuth();
  return (
    <header className="mb-8 flex flex-col gap-4 border-b border-slate-200 pb-6 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p className="text-xs font-semibold uppercase tracking-wider text-brand-600">Admin Panel</p>
        <h1 className="mt-1 text-2xl font-bold text-slate-900">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
      </div>
      <div className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-2 shadow-sm">
        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-800">
          {admin?.fullName?.charAt(0) || "A"}
        </div>
        <div className="text-right text-sm">
          <p className="font-medium text-slate-900">{admin?.fullName}</p>
          <p className="text-xs text-slate-500">{admin?.email}</p>
        </div>
      </div>
    </header>
  );
}
