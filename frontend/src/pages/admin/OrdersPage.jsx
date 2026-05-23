import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ordersApi } from "../../api/endpoints";
import AdminDetailList from "../../components/admin/AdminDetailList";
import AdminModal from "../../components/admin/AdminModal";
import AdminRowActions from "../../components/admin/AdminRowActions";
import Loader from "../../components/Loader";
import Pagination from "../../components/Pagination";
import StatusBadge from "../../components/StatusBadge";
import { DELIVERY_OPTIONS } from "../../constants/deliveryTypes";
import { ORDER_STATUSES } from "../../constants/orderStatuses";
import { useToast } from "../../context/ToastContext";
import { formatUsd } from "../../utils/orderPricing";

export default function OrdersPage() {
  const { show } = useToast();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [invoiceOrder, setInvoiceOrder] = useState(null);
  const [invoice, setInvoice] = useState({ productCost: 0, serviceFee: 5, otherCharges: 0 });
  const [modal, setModal] = useState(null);
  const [editForm, setEditForm] = useState({
    fullName: "",
    phone: "",
    email: "",
    productUrl: "",
    productName: "",
    productUnitPriceUsd: "",
    quantity: 1,
    status: "",
    statusNote: "",
    notes: "",
    addressDetail: "",
    deliveryType: "",
    districtId: "",
    orderScreenshotUrl: "",
  });
  const [saving, setSaving] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ["admin-orders", page, status, search],
    queryFn: () =>
      ordersApi.list({ page, pageSize: 10, status: status || undefined, search: search || undefined }),
  });

  const loadOrder = async (orderId) => ordersApi.get(orderId);

  const openView = async (orderId) => {
    try {
      const order = await loadOrder(orderId);
      setModal({ mode: "view", order });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const openEdit = async (orderId) => {
    try {
      const order = await loadOrder(orderId);
      setEditForm({
        fullName: order.customerFullName || "",
        phone: order.customerPhone || "",
        email: order.customerEmail || "",
        productUrl: order.productUrl || "",
        productName: order.productName || "",
        productUnitPriceUsd: order.productUnitPriceUsd ?? "",
        quantity: order.quantity,
        status: order.status,
        statusNote: "",
        notes: order.notes || "",
        addressDetail: order.addressDetail || "",
        deliveryType: order.deliveryType || "",
        districtId: order.districtId || "",
        orderScreenshotUrl: order.orderScreenshotUrl || "",
      });
      setModal({ mode: "edit", order });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const saveEdit = async () => {
    if (!modal?.order) return;
    setSaving(true);
    try {
      const qty = Number(editForm.quantity);
      if (!Number.isFinite(qty) || qty < 1) {
        show("Quantity waa inay ahaataa tiro ≥ 1.", "error");
        return;
      }
      const rawPrice = editForm.productUnitPriceUsd;
      const productUnitPriceUsd =
        rawPrice === "" || rawPrice === null || rawPrice === undefined
          ? undefined
          : Number(rawPrice);
      if (
        productUnitPriceUsd !== undefined &&
        (!Number.isFinite(productUnitPriceUsd) || productUnitPriceUsd <= 0)
      ) {
        show("Qiimaha alaabtu waa inuu ahaadaa tiro ka weyn eber.", "error");
        return;
      }

      await ordersApi.update(modal.order.orderId, {
        fullName: editForm.fullName.trim() || undefined,
        phone: editForm.phone.trim() || undefined,
        email: editForm.email.trim() || undefined,
        productUrl: editForm.productUrl.trim() || undefined,
        productName: editForm.productName,
        quantity: qty,
        productUnitPriceUsd,
        status: editForm.status || undefined,
        statusNote: editForm.statusNote.trim() || undefined,
        notes: editForm.notes,
        addressDetail: editForm.addressDetail.trim() || undefined,
        deliveryType: editForm.deliveryType || undefined,
        districtId: editForm.districtId.trim() || undefined,
        orderScreenshotUrl: editForm.orderScreenshotUrl.trim() || undefined,
      });
      show("Order updated", "success");
      setModal(null);
      qc.invalidateQueries({ queryKey: ["admin-orders"] });
    } catch (err) {
      show(err.message, "error");
    } finally {
      setSaving(false);
    }
  };

  const removeOrder = async (orderId) => {
    if (!window.confirm(`Delete order ${orderId}?`)) return;
    try {
      await ordersApi.remove(orderId);
      show("Order deleted", "success");
      qc.invalidateQueries({ queryKey: ["admin-orders"] });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const updateStatus = async (orderId, newStatus) => {
    try {
      await ordersApi.updateStatus(orderId, { status: newStatus });
      show("Status updated", "success");
      qc.invalidateQueries({ queryKey: ["admin-orders"] });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const createInvoice = async (orderId) => {
    try {
      await ordersApi.createInvoice(orderId, invoice);
      show("Invoice created", "success");
      qc.invalidateQueries({ queryKey: ["admin-orders"] });
      setInvoiceOrder(null);
    } catch (err) {
      show(err.message, "error");
    }
  };

  if (isLoading) return <Loader />;
  if (error) return <p className="text-red-600">{error.message}</p>;

  return (
    <div>
      <h1 className="text-2xl font-bold">Orders</h1>
      <p className="text-sm text-slate-500">API: GET · PUT · DELETE /api/orders</p>
      <div className="mt-4 flex flex-wrap gap-2">
        <input
          className="rounded-lg border px-3 py-2 text-sm"
          placeholder="Search..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          className="rounded-lg border px-3 py-2 text-sm"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          <option value="">All status</option>
          {ORDER_STATUSES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>
      <div className="mt-4 overflow-x-auto rounded-xl border bg-white">
        <table className="w-full text-left text-sm">
          <thead className="border-b bg-slate-800 text-white">
            <tr>
              <th className="p-3">Order ID</th>
              <th className="p-3">Product</th>
              <th className="p-3">User</th>
              <th className="p-3">Total</th>
              <th className="p-3">Status</th>
              <th className="p-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {data?.items?.map((o) => (
              <tr key={o.orderId} className="border-b hover:bg-slate-50/80">
                <td className="p-3 font-mono text-xs">{o.orderId}</td>
                <td className="p-3">{o.productName || "—"}</td>
                <td className="p-3">{o.userId}</td>
                <td className="p-3">{formatUsd(o.totalAmountUsd)}</td>
                <td className="p-3">
                  <StatusBadge status={o.status} />
                </td>
                <td className="p-3">
                  <div className="flex flex-col gap-2">
                    <AdminRowActions
                      compact
                      onView={() => openView(o.orderId)}
                      onEdit={() => openEdit(o.orderId)}
                      onDelete={() => removeOrder(o.orderId)}
                    />
                    <div className="flex flex-wrap items-center gap-2">
                      <button
                        type="button"
                        className="text-xs font-medium text-brand-600 hover:underline"
                        onClick={() => setInvoiceOrder(o)}
                      >
                        Invoice
                      </button>
                      <select
                        className="rounded border px-1 py-0.5 text-xs"
                        defaultValue=""
                        onChange={(e) => e.target.value && updateStatus(o.orderId, e.target.value)}
                      >
                        <option value="">Status…</option>
                        {ORDER_STATUSES.map((s) => (
                          <option key={s} value={s}>
                            {s}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination page={data?.page || 1} totalPages={data?.totalPages || 1} onPageChange={setPage} />

      {modal?.mode === "view" && modal.order && (
        <AdminModal title={`Order ${modal.order.orderId}`} onClose={() => setModal(null)}>
          <AdminDetailList
            rows={[
              { label: "User", value: modal.order.userId },
              { label: "Customer name", value: modal.order.customerFullName || "—" },
              { label: "Customer email", value: modal.order.customerEmail || "—" },
              { label: "Customer phone", value: modal.order.customerPhone || "—" },
              { label: "Product URL", value: modal.order.productUrl || "—" },
              { label: "Product", value: modal.order.productName },
              { label: "Unit price", value: formatUsd(modal.order.productUnitPriceUsd) },
              { label: "Qty", value: String(modal.order.quantity) },
              { label: "District", value: modal.order.districtName },
              { label: "Delivery", value: modal.order.deliveryType },
              { label: "Address", value: modal.order.addressDetail || "—" },
              { label: "Notes", value: modal.order.notes || "—" },
              { label: "Order screenshot URL", value: modal.order.orderScreenshotUrl || "—" },
              { label: "Total", value: formatUsd(modal.order.totalAmountUsd) },
              { label: "Status", value: modal.order.status },
              {
                label: "Created",
                value: new Date(modal.order.createdAt).toLocaleString(),
              },
            ]}
          />
        </AdminModal>
      )}

      {modal?.mode === "edit" && modal.order && (
        <AdminModal
          title={`Edit ${modal.order.orderId}`}
          onClose={() => setModal(null)}
          onSave={saveEdit}
          saving={saving}
          saveLabel="Save (PUT)"
        >
          <div className="max-h-[min(60vh,28rem)] space-y-4 overflow-y-auto pr-1">
            <p className="text-xs text-slate-500">
              PUT wuxuu u dirayaa dhammaan beeraha hoose si backend uu u cusboonaysiiyo xogta dalabka.
            </p>
            <label className="block text-xs font-medium text-slate-600">
              Magaca macmiilka
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.fullName}
                onChange={(e) => setEditForm({ ...editForm, fullName: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Telefoonka macmiilka
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.phone}
                onChange={(e) => setEditForm({ ...editForm, phone: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Email macmiilka
              <input
                type="email"
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.email}
                onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Product URL
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm font-mono text-xs"
                value={editForm.productUrl}
                onChange={(e) => setEditForm({ ...editForm, productUrl: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Product name
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.productName}
                onChange={(e) => setEditForm({ ...editForm, productName: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Qiimaha hal cutub (USD)
              <input
                type="number"
                step="0.01"
                min="0"
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.productUnitPriceUsd}
                onChange={(e) => setEditForm({ ...editForm, productUnitPriceUsd: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Quantity
              <input
                type="number"
                min={1}
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.quantity}
                onChange={(e) => setEditForm({ ...editForm, quantity: e.target.value })}
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
            <label className="block text-xs font-medium text-slate-600">
              Status note (ikhtiyaari)
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                placeholder="Marka status la beddelayo"
                value={editForm.statusNote}
                onChange={(e) => setEditForm({ ...editForm, statusNote: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Nooca gaarsiinta
              <select
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                value={editForm.deliveryType}
                onChange={(e) => setEditForm({ ...editForm, deliveryType: e.target.value })}
              >
                <option value="">—</option>
                {DELIVERY_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-xs font-medium text-slate-600">
              District ID
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm font-mono text-xs"
                value={editForm.districtId}
                onChange={(e) => setEditForm({ ...editForm, districtId: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Cinwaanka / faahfaahin
              <textarea
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                rows={2}
                value={editForm.addressDetail}
                onChange={(e) => setEditForm({ ...editForm, addressDetail: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Notes
              <textarea
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
                rows={2}
                value={editForm.notes}
                onChange={(e) => setEditForm({ ...editForm, notes: e.target.value })}
              />
            </label>
            <label className="block text-xs font-medium text-slate-600">
              Order screenshot URL
              <input
                className="mt-1 w-full rounded-lg border px-3 py-2 text-sm font-mono text-xs"
                value={editForm.orderScreenshotUrl}
                onChange={(e) => setEditForm({ ...editForm, orderScreenshotUrl: e.target.value })}
              />
            </label>
          </div>
        </AdminModal>
      )}

      {invoiceOrder && (
        <AdminModal
          title={`Invoice — ${invoiceOrder.orderId}`}
          onClose={() => setInvoiceOrder(null)}
          onSave={() => createInvoice(invoiceOrder.orderId)}
          saveLabel="Create invoice"
        >
          <input
            type="number"
            className="w-full rounded-lg border px-3 py-2 text-sm"
            placeholder="Product cost"
            onChange={(e) => setInvoice((i) => ({ ...i, productCost: +e.target.value }))}
          />
          <input
            type="number"
            className="w-full rounded-lg border px-3 py-2 text-sm"
            placeholder="Service fee"
            defaultValue={5}
            onChange={(e) => setInvoice((i) => ({ ...i, serviceFee: +e.target.value }))}
          />
        </AdminModal>
      )}
    </div>
  );
}
