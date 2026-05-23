import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ordersApi } from "../../api/endpoints";
import AdminModal from "./AdminModal";
import AdminRowActions from "./AdminRowActions";
import Loader from "../Loader";
import Pagination from "../Pagination";
import { ORDER_STATUSES } from "../../constants/orderStatuses";
import { useToast } from "../../context/ToastContext";
import { orderListRow } from "../../utils/orderListRow";

const PAGE_SIZE = 10;
const ALL_PAGE_SIZE = 200;

export default function DashboardOrdersBox() {
  const { show } = useToast();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [showAll, setShowAll] = useState(false);
  const [modal, setModal] = useState(null);
  const [editForm, setEditForm] = useState({
    productName: "",
    fullName: "",
    phone: "",
    status: "",
  });
  const [saving, setSaving] = useState(false);

  const pageSize = showAll ? ALL_PAGE_SIZE : PAGE_SIZE;
  const queryPage = showAll ? 1 : page;

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["dashboard-orders", queryPage, showAll, pageSize],
    queryFn: () => ordersApi.list({ page: queryPage, pageSize }),
    staleTime: 30_000,
  });

  const rawItems = (data?.items ?? data?.Items ?? []).map(orderListRow);
  const seen = new Set();
  const items = rawItems.filter((row) => {
    if (!row.orderId || seen.has(row.orderId)) return false;
    seen.add(row.orderId);
    return true;
  });
  const totalCount = data?.totalCount ?? data?.TotalCount ?? 0;
  const totalPages = Math.max(1, data?.totalPages ?? data?.TotalPages ?? 1);
  const currentPage = data?.page ?? data?.Page ?? queryPage;

  const toggleAll = () => {
    setShowAll((v) => !v);
    setPage(1);
  };

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ["dashboard-orders"] });
    qc.invalidateQueries({ queryKey: ["admin-orders"] });
  };

  const openEdit = (row) => {
    setEditForm({
      productName: row.productName || "",
      fullName: row.customerFullName || "",
      phone: row.customerPhone || "",
      status: row.status,
    });
    setModal({ mode: "edit", row });
  };

  const saveEdit = async () => {
    if (!modal?.row) return;
    setSaving(true);
    try {
      await ordersApi.update(modal.row.orderId, {
        productName: editForm.productName || undefined,
        fullName: editForm.fullName || undefined,
        phone: editForm.phone || undefined,
        status: editForm.status || undefined,
      });
      show("Updated", "success");
      setModal(null);
      invalidate();
    } catch (err) {
      show(err.message, "error");
    } finally {
      setSaving(false);
    }
  };

  const removeRow = async (orderId) => {
    if (!window.confirm(`Delete order ${orderId}?`)) return;
    try {
      await ordersApi.remove(orderId);
      show("Deleted", "success");
      invalidate();
    } catch (err) {
      show(err.message, "error");
    }
  };

  return (
    <section className="mt-10">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
          Recent orders
        </h2>
        <button
          type="button"
          onClick={toggleAll}
          className={`rounded-xl px-4 py-2 text-sm font-bold tracking-wide transition ${
            showAll
              ? "bg-brand-600 text-white shadow-md ring-2 ring-brand-300"
              : "border border-slate-300 bg-white text-slate-700 hover:border-brand-400 hover:text-brand-700"
          }`}
        >
          {showAll ? "10 kaliya" : "ALL"}
        </button>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        {isLoading ? (
          <div className="p-8">
            <Loader />
          </div>
        ) : (
          <>
            <div
              className={`overflow-x-auto ${showAll ? "max-h-[min(70vh,32rem)] overflow-y-auto" : ""}`}
            >
              <table className="w-full min-w-[640px] text-sm">
                <thead className="sticky top-0 z-10 border-b bg-slate-800 text-white">
                  <tr>
                    <th className="p-3 text-left font-semibold">User ID</th>
                    <th className="p-3 text-left font-semibold">Order ID</th>
                    <th className="p-3 text-left font-semibold">Full name</th>
                    <th className="p-3 text-left font-semibold">Phone</th>
                    <th className="p-3 text-left font-semibold">Product</th>
                    <th className="p-3 text-left font-semibold">Admin</th>
                  </tr>
                </thead>
                <tbody>
                  {items.length === 0 && (
                    <tr>
                      <td colSpan={6} className="p-8 text-center text-slate-500">
                        No orders yet.
                      </td>
                    </tr>
                  )}
                  {items.map((row, index) => {
                    const prev = items[index - 1];
                    const sameUser =
                      prev && prev.userId && prev.userId === row.userId;
                    return (
                      <tr
                        key={row.orderId}
                        className={`border-b last:border-0 hover:bg-slate-50/80 ${sameUser ? "bg-amber-50/40" : ""}`}
                      >
                        <td className="p-3 font-mono text-xs text-slate-600">
                          {sameUser ? (
                            <span className="text-slate-400" title="Isla macmiil — dalab kale">
                              ↳ {row.userId}
                            </span>
                          ) : (
                            row.userId
                          )}
                        </td>
                        <td className="p-3 font-mono text-xs font-semibold text-brand-800">
                          {row.orderId}
                          {sameUser && (
                            <span className="ml-1.5 rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-900">
                              dalab kale
                            </span>
                          )}
                        </td>
                        <td className="p-3">{row.customerFullName || "—"}</td>
                        <td className="p-3 whitespace-nowrap">{row.customerPhone || "—"}</td>
                        <td
                          className="max-w-[180px] truncate p-3"
                          title={row.productName || ""}
                        >
                          {row.productName || "—"}
                        </td>
                        <td className="p-3">
                          <AdminRowActions
                            compact
                            onEdit={() => openEdit(row)}
                            onDelete={() => removeRow(row.orderId)}
                          />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {isFetching && !isLoading && (
              <p className="border-t bg-slate-50 px-3 py-1 text-center text-[10px] text-slate-400">
                Updating…
              </p>
            )}

            {!showAll && totalPages > 1 && (
              <div className="border-t bg-slate-50/80">
                <Pagination
                  page={currentPage}
                  totalPages={totalPages}
                  onPageChange={setPage}
                />
              </div>
            )}
          </>
        )}
      </div>

      {modal?.mode === "edit" && modal.row && (
        <AdminModal
          title={`Edit ${modal.row.orderId}`}
          onClose={() => setModal(null)}
          onSave={saveEdit}
          saving={saving}
          saveLabel="Save (PUT)"
        >
          <label className="block text-xs font-medium text-slate-600">
            Full name
            <input
              className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
              value={editForm.fullName}
              onChange={(e) => setEditForm({ ...editForm, fullName: e.target.value })}
            />
          </label>
          <label className="block text-xs font-medium text-slate-600">
            Phone
            <input
              className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
              value={editForm.phone}
              onChange={(e) => setEditForm({ ...editForm, phone: e.target.value })}
            />
          </label>
          <label className="block text-xs font-medium text-slate-600">
            Product
            <input
              className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
              value={editForm.productName}
              onChange={(e) => setEditForm({ ...editForm, productName: e.target.value })}
            />
          </label>
          <label className="block text-xs font-medium text-slate-600">
            Status
            <select
              className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
              value={editForm.status}
              onChange={(e) => setEditForm({ ...editForm, status: e.target.value })}
            >
              {ORDER_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </label>
        </AdminModal>
      )}
    </section>
  );
}
