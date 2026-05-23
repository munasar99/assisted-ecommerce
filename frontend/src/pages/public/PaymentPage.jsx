import { Link } from "react-router-dom";
import PageShell from "../../components/PageShell";

export default function PaymentPage() {
  return (
    <PageShell title="Lacag Bixinta" subtitle="Payment" centered maxWidth="max-w-md">
      <div className="card-elevated flex min-h-[280px] flex-col items-center justify-center p-10 sm:p-14">
        <Link
          to="/order"
          className="group relative inline-flex w-full max-w-xs items-center justify-center gap-3 overflow-hidden rounded-2xl bg-gradient-to-br from-emerald-500 via-brand-600 to-teal-700 px-8 py-5 text-lg font-bold text-white shadow-xl shadow-brand-600/40 transition duration-300 hover:scale-[1.02] hover:shadow-2xl hover:shadow-brand-600/50 active:scale-[0.98]"
        >
          <span
            className="pointer-events-none absolute inset-0 bg-gradient-to-t from-white/0 via-white/10 to-white/25 opacity-0 transition group-hover:opacity-100"
            aria-hidden
          />
          <svg
            className="relative h-6 w-6 shrink-0"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"
            />
          </svg>
          <span className="relative tracking-wide">Order</span>
        </Link>
      </div>
    </PageShell>
  );
}
