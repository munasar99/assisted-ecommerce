import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { deliveryApi } from "../../api/endpoints";
import AdminDetailList from "../../components/admin/AdminDetailList";
import AdminModal from "../../components/admin/AdminModal";
import AdminRowActions from "../../components/admin/AdminRowActions";
import Loader from "../../components/Loader";
import { useToast } from "../../context/ToastContext";

const emptyCreateForm = {
  zoneId: "",
  districtName: "",
  districtNameEn: "",
  feeUsd: "",
  sortOrder: "0",
  isActive: true,
};

function invalidateZones(qc) {
  qc.invalidateQueries({ queryKey: ["zones-all"] });
  qc.invalidateQueries({ queryKey: ["zones"] });
}

export default function DeliveryPage() {
  const { show } = useToast();
  const qc = useQueryClient();
  const [modal, setModal] = useState(null);
  const [createForm, setCreateForm] = useState(emptyCreateForm);
  const [editForm, setEditForm] = useState({
    districtName: "",
    districtNameEn: "",
    feeUsd: "",
    isActive: true,
    sortOrder: "0",
  });
  const [saving, setSaving] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ["zones-all"],
    queryFn: deliveryApi.all,
  });

  const openCreate = () => {
    setCreateForm(emptyCreateForm);
    setModal({ mode: "create" });
  };

  const openView = async (zoneId) => {
    try {
      const zone = await deliveryApi.get(zoneId);
      setModal({ mode: "view", zone });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const openEdit = async (zoneId) => {
    try {
      const zone = await deliveryApi.get(zoneId);
      setEditForm({
        districtName: zone.districtName || "",
        districtNameEn: zone.districtNameEn || "",
        feeUsd: String(zone.feeUsd ?? ""),
        isActive: Boolean(zone.isActive),
        sortOrder: String(zone.sortOrder ?? 0),
      });
      setModal({ mode: "edit", zone });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const saveCreate = async () => {
    const zoneId = createForm.zoneId.trim().toUpperCase();
    if (!zoneId) {
      show("Zone ID waa loo baahan yahay (tusaale ZONE-HODAN).", "error");
      return;
    }
    if (!createForm.districtName.trim()) {
      show("Magaca degmada (SO) waa loo baahan yahay.", "error");
      return;
    }
    const fee = Number(createForm.feeUsd);
    const sort = Number(createForm.sortOrder);
    if (!Number.isFinite(fee) || fee < 0) {
      show("Fee waa inuu ahaadaa tiro ≥ 0.", "error");
      return;
    }
    if (!Number.isFinite(sort)) {
      show("Sort order waa inuu ahaadaa tiro.", "error");
      return;
    }
    setSaving(true);
    try {
      await deliveryApi.create({
        zoneId,
        districtName: createForm.districtName.trim(),
        districtNameEn: createForm.districtNameEn.trim() || createForm.districtName.trim(),
        feeUsd: fee,
        isActive: createForm.isActive,
        sortOrder: Math.trunc(sort),
      });
      show("Delivery zone created", "success");
      setModal(null);
      invalidateZones(qc);
    } catch (err) {
      show(err.message, "error");
    } finally {
      setSaving(false);
    }
  };

  const saveEdit = async () => {
    if (!modal?.zone) return;
    if (!editForm.districtName.trim()) {
      show("Magaca degmada (SO) waa loo baahan yahay.", "error");
      return;
    }
    const fee = Number(editForm.feeUsd);
    const sort = Number(editForm.sortOrder);
    if (!Number.isFinite(fee) || fee < 0) {
      show("Fee waa inuu ahaadaa tiro ≥ 0.", "error");
      return;
    }
    if (!Number.isFinite(sort)) {
      show("Sort order waa inuu ahaadaa tiro.", "error");
      return;
    }
    setSaving(true);
    try {
      await deliveryApi.update(modal.zone.zoneId, {
        districtName: editForm.districtName.trim(),
        districtNameEn: editForm.districtNameEn.trim(),
        feeUsd: fee,
        isActive: editForm.isActive,
        sortOrder: Math.trunc(sort),
      });
      show("Zone updated", "success");
      setModal(null);
      invalidateZones(qc);
    } catch (err) {
      show(err.message, "error");
    } finally {
      setSaving(false);
    }
  };

  const removeZone = async (zoneId) => {
    if (!window.confirm(`Delete zone ${zoneId}?`)) return;
    try {
      await deliveryApi.remove(zoneId);
      show("Zone deleted", "success");
      invalidateZones(qc);
    } catch (err) {
      show(err.message, "error");
    }
  };

  const saveFee = async (id, feeUsd) => {
    const fee = Number(feeUsd);
    if (!Number.isFinite(fee) || fee < 0) {
      show("Fee waa inuu ahaadaa tiro ≥ 0.", "error");
      return;
    }
    try {
      await deliveryApi.updateFee(id, fee);
      show("Fee updated", "success");
      invalidateZones(qc);
    } catch (err) {
      show(err.message, "error");
    }
  };

  const toggle = async (id) => {
    try {
      await deliveryApi.toggle(id);
      invalidateZones(qc);
    } catch (err) {
      show(err.message, "error");
    }
  };

  if (isLoading) return <Loader />;
  if (error) return <p className="text-red-600">{error.message}</p>;

  return (
    <div>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold">Delivery Zones</h1>
          <p className="text-sm text-slate-500">
            API: GET · POST · PUT · DELETE /api/delivery/zones
          </p>
        </div>
        <button type="button" className="btn-primary px-4 py-2 text-sm" onClick={openCreate}>
          + Create zone
        </button>
      </div>
      <div className="mt-4 overflow-x-auto rounded-xl border bg-white">
        <table className="w-full text-sm">
          <thead className="border-b bg-slate-800 text-white">
            <tr>
              <th className="p-3 text-left">Zone ID</th>
              <th className="p-3 text-left">District</th>
              <th className="p-3 text-left">Fee USD</th>
              <th className="p-3 text-left">Active</th>
              <th className="p-3 text-left">Quick</th>
              <th className="p-3 text-left">Actions</th>
            </tr>
          </thead>
          <tbody>
            {data?.map((z) => (
              <ZoneRow
                key={z.zoneId}
                zone={z}
                onSave={saveFee}
                onToggle={toggle}
                onView={() => openView(z.zoneId)}
                onEdit={() => openEdit(z.zoneId)}
                onDelete={() => removeZone(z.zoneId)}
              />
            ))}
          </tbody>
        </table>
      </div>

      {modal?.mode === "create" && (
        <AdminModal
          title="Create delivery zone"
          onClose={() => setModal(null)}
          onSave={saveCreate}
          saving={saving}
          saveLabel="Create (POST)"
        >
          <ZoneFormFields
            form={createForm}
            setForm={setCreateForm}
            showZoneId
          />
        </AdminModal>
      )}

      {modal?.mode === "view" && modal.zone && (
        <AdminModal title={`Zone ${modal.zone.zoneId}`} onClose={() => setModal(null)}>
          <AdminDetailList
            rows={[
              { label: "Zone ID", value: modal.zone.zoneId },
              { label: "District", value: modal.zone.districtName },
              { label: "English", value: modal.zone.districtNameEn },
              { label: "Fee", value: `$${modal.zone.feeUsd}` },
              { label: "Active", value: modal.zone.isActive ? "Yes" : "No" },
              { label: "Sort", value: String(modal.zone.sortOrder) },
            ]}
          />
        </AdminModal>
      )}

      {modal?.mode === "edit" && modal.zone && (
        <AdminModal
          title={`Edit ${modal.zone.zoneId}`}
          onClose={() => setModal(null)}
          onSave={saveEdit}
          saving={saving}
          saveLabel="Save (PUT)"
        >
          <div className="max-h-[min(60vh,24rem)] space-y-4 overflow-y-auto pr-1">
            <p className="text-xs text-slate-500">
              PUT wuxuu cusboonaysiinayaa magacyada, fee, active, iyo taxana liiska (sortOrder).
            </p>
            <ZoneFormFields form={editForm} setForm={setEditForm} zoneId={modal.zone.zoneId} />
          </div>
        </AdminModal>
      )}
    </div>
  );
}

function ZoneFormFields({ form, setForm, showZoneId, zoneId }) {
  return (
    <>
      {showZoneId ? (
        <label className="block text-xs font-medium text-slate-600">
          Zone ID *
          <input
            className="mt-1 w-full rounded-lg border px-3 py-2 font-mono text-sm"
            placeholder="ZONE-HODAN"
            value={form.zoneId}
            onChange={(e) => setForm({ ...form, zoneId: e.target.value.toUpperCase() })}
          />
        </label>
      ) : (
        <p className="rounded-lg bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600">{zoneId}</p>
      )}
      <label className="block text-xs font-medium text-slate-600">
        District (SO) *
        <input
          className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
          value={form.districtName}
          onChange={(e) => setForm({ ...form, districtName: e.target.value })}
        />
      </label>
      <label className="block text-xs font-medium text-slate-600">
        District (EN)
        <input
          className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
          value={form.districtNameEn}
          onChange={(e) => setForm({ ...form, districtNameEn: e.target.value })}
        />
      </label>
      <label className="block text-xs font-medium text-slate-600">
        Fee USD *
        <input
          type="number"
          step="0.01"
          min="0"
          className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
          value={form.feeUsd}
          onChange={(e) => setForm({ ...form, feeUsd: e.target.value })}
        />
      </label>
      <label className="block text-xs font-medium text-slate-600">
        Sort order (liiska)
        <input
          type="number"
          step="1"
          className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
          value={form.sortOrder}
          onChange={(e) => setForm({ ...form, sortOrder: e.target.value })}
        />
      </label>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={form.isActive}
          onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
        />
        Active
      </label>
    </>
  );
}

function ZoneRow({ zone, onSave, onToggle, onView, onEdit, onDelete }) {
  const [fee, setFee] = useState(String(zone.feeUsd));
  return (
    <tr className="border-b hover:bg-slate-50/80">
      <td className="p-3 font-mono text-xs">{zone.zoneId}</td>
      <td className="p-3">{zone.districtName}</td>
      <td className="p-3">
        <input className="w-20 rounded border px-2 py-1" value={fee} onChange={(e) => setFee(e.target.value)} />
      </td>
      <td className="p-3">
        <button
          type="button"
          onClick={() => onToggle(zone.zoneId)}
          className={zone.isActive ? "font-medium text-emerald-600" : "text-slate-400"}
        >
          {zone.isActive ? "Active" : "Off"}
        </button>
      </td>
      <td className="p-3">
        <button type="button" className="text-xs font-medium text-brand-600" onClick={() => onSave(zone.zoneId, fee)}>
          Save fee
        </button>
      </td>
      <td className="p-3">
        <AdminRowActions compact onView={onView} onEdit={onEdit} onDelete={onDelete} />
      </td>
    </tr>
  );
}
