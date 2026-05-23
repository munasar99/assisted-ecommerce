import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { analyticsApi } from "../../api/endpoints";
import DashboardOrdersBox from "../../components/admin/DashboardOrdersBox";
import {
  IconAnalytics,
  IconDelivery,
  IconOrders,
  IconOverview,
  IconPayments,
  IconUsers,
} from "../../components/admin/AdminIcons";
import { useAuth } from "../../context/AuthContext";

const ANALYTICS_STALE_MS = 2 * 60 * 1000;

const stats = [
  {
    key: "totalOrders",
    label: "Total Orders",
    color: "border-l-slate-600",
    bg: "bg-slate-100 text-slate-600",
    Icon: IconOrders,
  },
  {
    key: "pendingOrders",
    label: "Pending",
    color: "border-l-amber-500",
    bg: "bg-amber-100 text-amber-700",
    Icon: IconOverview,
  },
  {
    key: "paymentReviewOrders",
    label: "Payment Review",
    color: "border-l-violet-500",
    bg: "bg-violet-100 text-violet-700",
    Icon: IconPayments,
  },
  {
    key: "deliveredOrders",
    label: "Delivered",
    color: "border-l-emerald-500",
    bg: "bg-emerald-100 text-emerald-700",
    Icon: IconDelivery,
  },
  {
    key: "activeUsers",
    label: "Active Users",
    color: "border-l-sky-500",
    bg: "bg-sky-100 text-sky-700",
    Icon: IconUsers,
  },
];

function formatUsd(value) {
  return `$${Number(value ?? 0).toFixed(2)}`;
}

const quick = [
  { to: "/admin/orders", label: "Orders", sub: "View & update status", Icon: IconOrders },
  { to: "/admin/payments", label: "Payments", sub: "Confirm screenshots", Icon: IconPayments },
  { to: "/admin/users", label: "User Management", sub: "Create & edit customers", Icon: IconUsers },
  { to: "/admin/delivery", label: "Delivery", sub: "Zone fees", Icon: IconDelivery },
];

function StatSkeleton({ color, bg, Icon }) {
  return (
    <div
      className={`flex items-start gap-4 rounded-xl border border-slate-200 border-l-4 bg-white p-5 shadow-sm ${color}`}
    >
      <span className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${bg}`}>
        <Icon />
      </span>
      <div className="min-w-0 flex-1">
        <div className="h-3 w-24 animate-pulse rounded bg-slate-200" />
        <div className="mt-3 h-8 w-16 animate-pulse rounded bg-slate-200" />
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const { isAuthenticated, ready } = useAuth();
  const { data, isLoading, error } = useQuery({
    queryKey: ["analytics"],
    queryFn: analyticsApi.dashboard,
    enabled: ready && isAuthenticated,
    staleTime: ANALYTICS_STALE_MS,
    gcTime: 5 * 60 * 1000,
  });

  return (
    <>
      {error && (
        <p className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
          {error.message}
        </p>
      )}

      <section className="mb-6 grid gap-4 sm:grid-cols-2">
        <Link
          to="/admin/users"
          className="group flex items-start gap-4 rounded-2xl border border-indigo-200 border-l-4 border-l-indigo-500 bg-gradient-to-br from-indigo-50 to-white p-6 shadow-sm transition hover:border-indigo-300 hover:shadow-md"
        >
          <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-indigo-100 text-indigo-700">
            <IconUsers />
          </span>
          <div className="min-w-0 flex-1">
            <p className="text-xs font-semibold uppercase tracking-wide text-indigo-600">User Management</p>
            <p className="mt-1 text-3xl font-bold text-slate-900">
              {isLoading || !data ? "—" : data.totalUsers}
            </p>
            <p className="mt-2 text-sm text-slate-500 group-hover:text-indigo-700">Manage users →</p>
          </div>
        </Link>
        <div className="flex items-start gap-4 rounded-2xl border border-brand-200 border-l-4 border-l-brand-600 bg-gradient-to-br from-brand-50 to-white p-6 shadow-sm">
          <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-brand-100 text-brand-700">
            <IconAnalytics />
          </span>
          <div className="min-w-0">
            <p className="text-xs font-semibold uppercase tracking-wide text-brand-700">Total Revenue</p>
            <p className="mt-1 text-3xl font-bold text-slate-900">
              {isLoading || !data ? "—" : formatUsd(data.revenue)}
            </p>
            <p className="mt-2 text-sm text-slate-500">Dakhliga guud (invoices)</p>
          </div>
        </div>
      </section>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {stats.map(({ key, label, color, bg, Icon, format }) =>
          isLoading || !data ? (
            <StatSkeleton key={key} color={color} bg={bg} Icon={Icon} />
          ) : (
            <div
              key={key}
              className={`flex items-start gap-4 rounded-xl border border-slate-200 border-l-4 bg-white p-5 shadow-sm ${color}`}
            >
              <span className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${bg}`}>
                <Icon />
              </span>
              <div className="min-w-0">
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</p>
                <p className="mt-1 text-3xl font-bold text-slate-900">
                  {format ? format(data[key]) : data[key]}
                </p>
              </div>
            </div>
          ),
        )}
      </div>

      <DashboardOrdersBox />

      <section className="mt-10">
        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
          Quick actions
        </h2>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {quick.map(({ to, label, sub, Icon }) => (
            <Link
              key={to}
              to={to}
              className="flex items-start gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand-300 hover:shadow-md"
            >
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
                <Icon />
              </span>
              <span>
                <p className="font-semibold text-slate-900">{label}</p>
                <p className="mt-1 text-xs text-slate-500">{sub}</p>
              </span>
            </Link>
          ))}
        </div>
      </section>
    </>
  );
}
