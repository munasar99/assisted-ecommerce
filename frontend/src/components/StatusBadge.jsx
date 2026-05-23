import { STATUS_LABELS } from "../constants/orderStatuses";

const colors = {
  Pending: "bg-amber-100 text-amber-800",
  InvoiceSent: "bg-blue-100 text-blue-800",
  WaitingPayment: "bg-orange-100 text-orange-800",
  PaymentReview: "bg-purple-100 text-purple-800",
  Confirmed: "bg-emerald-100 text-emerald-800",
  OrderedFromSupplier: "bg-cyan-100 text-cyan-800",
  Shipping: "bg-indigo-100 text-indigo-800",
  ArrivedMogadishu: "bg-teal-100 text-teal-800",
  OutForDelivery: "bg-sky-100 text-sky-800",
  Delivered: "bg-green-100 text-green-800",
};

export default function StatusBadge({ status }) {
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${colors[status] || "bg-slate-100 text-slate-700"}`}
    >
      {STATUS_LABELS[status] || status}
    </span>
  );
}
