import { useAuth } from "../context/AuthContext";

export default function AdminTopBar({ title, actions }) {
  const { admin } = useAuth();
  return (
    <header className="sticky top-0 z-20 -mx-6 -mt-6 mb-6 flex items-center justify-between border-b border-slate-200 bg-white/90 px-6 py-4 backdrop-blur-md lg:-mx-8 lg:-mt-8 lg:px-8">
      <h1 className="text-lg font-semibold text-slate-900">{title}</h1>
      <div className="flex items-center gap-3">
        {actions}
        <span className="hidden text-sm text-slate-500 sm:inline">{admin?.email}</span>
        <span className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-800">
          {admin?.fullName?.charAt(0) || "A"}
        </span>
      </div>
    </header>
  );
}
