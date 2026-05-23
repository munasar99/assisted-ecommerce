import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { paymentsApi } from "../../api/endpoints";
import { mediaUrl } from "../../api/mediaUrl";
import AdminDetailList from "../../components/admin/AdminDetailList";
import AdminModal from "../../components/admin/AdminModal";
import AdminRowActions from "../../components/admin/AdminRowActions";
import Loader from "../../components/Loader";
import Pagination from "../../components/Pagination";
import StatusBadge from "../../components/StatusBadge";
import { useToast } from "../../context/ToastContext";
import { formatUsd } from "../../utils/orderPricing";

const PAYMENT_STATUSES = ["UnderReview", "Confirmed", "Rejected", "Pending"];

export default function PaymentsPage() {
  const { show } = useToast();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState("");
  const [modal, setModal] = useState(null);
  const [editForm, setEditForm] = useState({
    payerPhone: "",
    paymentMethod: "",
    amountUsd: "",
    screenshotUrl: "",
    status: "UnderReview",
  });
  const [saving, setSaving] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ["payments-admin", page, statusFilter],
    queryFn: () =>
      paymentsApi.list({
        page,
        pageSize: 10,
        status: statusFilter || undefined,
      }),
  });

  const openView = async (paymentId) => {
    try {
      const payment = await paymentsApi.get(paymentId);
      setModal({ mode: "view", payment });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const openEdit = async (paymentId) => {
    try {
      const payment = await paymentsApi.get(paymentId);
      setEditForm({
        payerPhone: payment.payerPhone || "",
        paymentMethod: payment.paymentMethod || "",
        amountUsd: String(payment.amountUsd),
        screenshotUrl: payment.screenshotUrl || "",
        status: payment.status,
      });
      setModal({ mode: "edit", payment });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const saveEdit = async () => {
    if (!modal?.payment) return;
    setSaving(true);
    try {
      const amt = Number(editForm.amountUsd);
      if (!Number.isFinite(amt) || amt <= 0) {
        show("Lacagta (USD) waa inay ahaataa tiro ka weyn eber.", "error");
        return;
      }

      await paymentsApi.update(modal.payment.paymentId, {
        payerPhone: editForm.payerPhone.trim() || undefined,
        paymentMethod: editForm.paymentMethod.trim(),
        amountUsd: amt,
        screenshotUrl: editForm.screenshotUrl.trim(),
        status: editForm.status,
      });
      show("Payment updated", "success");
      setModal(null);
      qc.invalidateQueries({ queryKey: ["payments-admin"] });
    } catch (err) {
      show(err.message, "error");
    } finally {
      setSaving(false);
    }
  };

  const removePayment = async (paymentId) => {
    if (!window.confirm(`Delete payment ${paymentId}?`)) return;
    try {
      await paymentsApi.remove(paymentId);
      show("Payment deleted", "success");
      qc.invalidateQueries({ queryKey: ["payments-admin"] });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const quickStatus = async (paymentId, status) => {
    try {
      await paymentsApi.update(paymentId, { status });
      show(status === "Confirmed" ? "Confirmed" : "Rejected", "success");
      qc.invalidateQueries({ queryKey: ["payments-admin"] });
    } catch (err) {
      show(err.message, "error");
    }
  };

  if (isLoading) return <Loader />;
  if (error) return <p className="text-red-600">{error.message}</p>;

  return (
    <div>
      <h1 className="text-2xl font-bold">Payments</h1>
      <p className="text-sm text-slate-500">API: GET · PUT · DELETE /api/payments</p>
      <select
        className="mt-4 rounded-lg border px-3 py-2 text-sm"
        value={statusFilter}
        onChange={(e) => {
          setStatusFilter(e.target.value);
          setPage(1);
        }}
      >
        <option value="">All statuses</option>
        {PAYMENT_STATUSES.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </select>

      <div className="mt-4 overflow-x-auto rounded-xl border bg-white">
        <table className="w-full text-sm">
          <thead className="border-b bg-slate-800 text-white">
            <tr>
              <th className="p-3 text-left">Payment ID</th>
              <th className="p-3 text-left">Order</th>
              <th className="p-3 text-left">Amount</th>
              <th className="p-3 text-left">Status</th>
              <th className="p-3 text-left">Actions</th>
            </tr>
          </thead>
          <tbody>
            {data?.items?.length === 0 && (
              <tr>
                <td colSpan={5} className="p-6 text-center text-slate-500">
                  No payments found.
                </td>
              </tr>
            )}
            {data?.items?.map((p) => (
              <tr key={p.paymentId} className="border-b hover:bg-slate-50/80">
                <td className="p-3 font-mono text-xs">{p.paymentId}</td>
                <td className="p-3 font-mono text-xs text-slate-600">{p.orderId}</td>
                <td className="p-3">{formatUsd(p.amountUsd)}</td>
                <td className="p-3">
                  <StatusBadge status={p.status} />
                </td>
                <td className="p-3">
                  <div className="flex flex-col gap-2">
                    <AdminRowActions
                      compact
                      onView={() => openView(p.paymentId)}
                      onEdit={() => openEdit(p.paymentId)}
                      onDelete={() => removePayment(p.paymentId)}
                    />
                    {p.status === "UnderReview" && (
                      <div className="flex gap-1">
                        <button
                          type="button"
                          className="rounded bg-emerald-600 px-2 py-1 text-[10px] font-semibold text-white"
                          onClick={() => quickStatus(p.paymentId, "Confirmed")}
                        >
                          Confirm
                        </button>
                        <button
                          type="button"
                          className="rounded bg-red-100 px-2 py-1 text-[10px] font-semibold text-red-700"
                          onClick={() => quickStatus(p.paymentId, "Rejected")}
                        >
                          Reject
                        </button>
                      </div>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination page={data?.page || 1} totalPages={data?.totalPages || 1} onPageChange={setPage} />

      {modal?.mode === "view" && modal.payment && (
        <AdminModal title={`Payment ${modal.payment.paymentId}`} onClose={() => setModal(null)}>
          <AdminDetailList
            rows={[
              { label: "Order", value: modal.payment.orderId },
              { label: "User", value: modal.payment.userId },
              { label: "Amount", value: formatUsd(modal.payment.amountUsd) },
              { label: "Phone", value: modal.payment.payerPhone || "—" },
              { label: "Method", value: modal.payment.paymentMethod || "—" },
              { label: "Screenshot path", value: modal.payment.screenshotUrl || "—" },
              { label: "Status", value: modal.payment.status },
              {
                label: "Created",
                value: new Date(modal.payment.createdAt).toLocaleString(),
              },
              {
                label: "Updated",
                value: new Date(modal.payment.updatedAt).toLocaleString(),
              },
            ]}
          />
          {modal.payment.screenshotUrl && (
            <a href={mediaUrl(modal.payment.screenshotUrl)} target="_blank" rel="noreferrer">
              <img
                src={mediaUrl(modal.payment.screenshotUrl)}
                alt="Screenshot"
                className="mt-3 max-h-48 rounded-lg border object-contain"
              />
            </a>
          )}
        </AdminModal>
      )}

      {modal?.mode === "edit" && modal.payment && (
        <AdminModal
          title={`Edit ${modal.payment.paymentId}`}
          onClose={() => setModal(null)}
          onSave={saveEdit}
          saving={saving}
          saveLabel="Save (PUT)"
        >
          <div className="max-h-[min(60vh,28rem)] space-y-4 overflow-y-auto pr-1">
            <p className="text-xs text-slate-500">
              PUT: lacag, habka lacag bixinta, telefoonka bixiyaha, screenshot URL, iyo status.
            </p>
            <p className="rounded-lg bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600">
              Order: {modal.payment.orderId} · User: {modal.payment.userId}
            </p>
            <label className="block text-xs font-medium text-slate-600">
              Payer phone
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.payerPhone}
                onChange={(e) => setEditForm({ ...editForm, payerPhone: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Method
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.paymentMethod}
                onChange={(e) => setEditForm({ ...editForm, paymentMethod: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Amount USD
              <input
                type="number"
                step="0.01"
                min="0"
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.amountUsd}
                onChange={(e) => setEditForm({ ...editForm, amountUsd: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Screenshot URL / path
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 font-mono text-xs"
                placeholder="Ku dar ama beddel URL-ka server-ka"
                value={editForm.screenshotUrl}
                onChange={(e) => setEditForm({ ...editForm, screenshotUrl: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Status
              <select
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.status}
                onChange={(e) => setEditForm({ ...editForm, status: e.target.value })}
              >
                {PAYMENT_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </AdminModal>
      )}
    </div>
  );
}
