import { NavLink } from "react-router-dom";
import Logo from "./Logo";
import {
  IconAnalytics,
  IconDelivery,
  IconLogout,
  IconOrders,
  IconOverview,
  IconPayments,
  IconSettings,
  IconUsers,
} from "./admin/AdminIcons";
import { useAuth } from "../context/AuthContext";

const links = [
  { to: "/admin/dashboard", label: "Overview", Icon: IconOverview },
  { to: "/admin/orders", label: "Orders", Icon: IconOrders },
  { to: "/admin/users", label: "Users", Icon: IconUsers },
  { to: "/admin/payments", label: "Payments", Icon: IconPayments },
  { to: "/admin/delivery", label: "Delivery", Icon: IconDelivery },
  { to: "/admin/analytics", label: "Analytics", Icon: IconAnalytics },
  { to: "/admin/settings", label: "Settings", Icon: IconSettings },
];

function NavIcon({ Icon, active }) {
  return (
    <span
      className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg transition ${
        active ? "bg-brand-600 text-white shadow-sm" : "bg-slate-100 text-slate-500 group-hover:bg-slate-200 group-hover:text-slate-700"
      }`}
    >
      <Icon />
    </span>
  );
}

export default function Sidebar() {
  const { admin, logout } = useAuth();

  return (
    <aside className="fixed inset-y-0 left-0 z-40 flex h-screen w-64 flex-col border-r border-slate-200 bg-white shadow-sm">
      <div className="shrink-0 border-b border-slate-100 p-4">
        <Logo compact />
        <p className="mt-2 text-[10px] font-semibold uppercase tracking-wider text-slate-400">
          Admin Panel
        </p>
      </div>
      <nav
        className="min-h-0 flex-1 space-y-0.5 overflow-y-auto overscroll-contain p-3"
        aria-label="Admin"
      >
        {links.map(({ to, label, Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `group flex items-center gap-3 rounded-xl px-2.5 py-2 text-sm font-medium transition ${
                isActive
                  ? "bg-brand-50 text-brand-900 ring-1 ring-brand-200/70"
                  : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
              }`
            }
          >
            {({ isActive }) => (
              <>
                <NavIcon Icon={Icon} active={isActive} />
                <span className="truncate">{label}</span>
              </>
            )}
          </NavLink>
        ))}
      </nav>
      <div className="shrink-0 border-t border-slate-100 bg-white p-4">
        <p className="truncate text-xs font-medium text-slate-800">{admin?.fullName}</p>
        <p className="truncate text-[10px] text-slate-500">{admin?.email}</p>
        <button
          type="button"
          onClick={logout}
          className="mt-3 flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 py-2.5 text-sm font-medium text-slate-600 transition hover:border-red-200 hover:bg-red-50 hover:text-red-700"
        >
          <IconLogout />
          Logout
        </button>
      </div>
    </aside>
  );
}
