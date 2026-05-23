import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { usersApi } from "../../api/endpoints";
import AdminDetailList from "../../components/admin/AdminDetailList";
import AdminModal from "../../components/admin/AdminModal";
import AdminRowActions from "../../components/admin/AdminRowActions";
import Loader from "../../components/Loader";
import Pagination from "../../components/Pagination";
import { useToast } from "../../context/ToastContext";
import { dedupeUsersByPhone } from "../../utils/phone";

export default function UsersPage() {
  const { show } = useToast();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [form, setForm] = useState({ fullName: "", phone: "" });
  const [modal, setModal] = useState(null);
  const [editForm, setEditForm] = useState({ fullName: "", phone: "", status: "active" });
  const [saving, setSaving] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ["users", page],
    queryFn: () => usersApi.list({ page, pageSize: 10 }),
  });

  const create = async (e) => {
    e.preventDefault();
    try {
      await usersApi.create(form);
      show("User created", "success");
      setForm({ fullName: "", phone: "" });
      qc.invalidateQueries({ queryKey: ["users"] });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const openView = async (userId) => {
    try {
      const user = await usersApi.get(userId);
      setModal({ mode: "view", user });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const openEdit = async (userId) => {
    try {
      const user = await usersApi.get(userId);
      setEditForm({ fullName: user.fullName, phone: user.phone, status: user.status });
      setModal({ mode: "edit", user });
    } catch (err) {
      show(err.message, "error");
    }
  };

  const saveEdit = async () => {
    if (!modal?.user) return;
    setSaving(true);
    try {
      await usersApi.update(modal.user.userId, editForm);
      show("User updated", "success");
      setModal(null);
      qc.invalidateQueries({ queryKey: ["users"] });
    } catch (err) {
      show(err.message, "error");
    } finally {
      setSaving(false);
    }
  };

  const removeUser = async (userId) => {
    if (!window.confirm(`Delete user ${userId}?`)) return;
    try {
      await usersApi.remove(userId);
      show("User deleted", "success");
      qc.invalidateQueries({ queryKey: ["users"] });
    } catch (err) {
      show(err.message, "error");
    }
  };

  if (isLoading) return <Loader />;
  if (error) return <p className="text-red-600">{error.message}</p>;

  const users = dedupeUsersByPhone(data?.items);

  return (
    <div>
      <h1 className="text-2xl font-bold">Users</h1>
      <p className="text-sm text-slate-500">API: GET · POST · PUT · DELETE /api/users</p>
      <form onSubmit={create} className="mt-4 flex flex-wrap gap-2 rounded-xl border bg-white p-4">
        <input
          className="rounded-lg border px-3 py-2 text-sm"
          placeholder="Full name"
          value={form.fullName}
          onChange={(e) => setForm({ ...form, fullName: e.target.value })}
        />
        <input
          className="rounded-lg border px-3 py-2 text-sm"
          placeholder="Phone"
          value={form.phone}
          onChange={(e) => setForm({ ...form, phone: e.target.value })}
        />
        <button type="submit" className="btn-primary px-4 py-2 text-sm">
          Create User
        </button>
      </form>
      <p className="mt-2 text-xs text-slate-500">
        Hal telefoon = hal user (01, 02…). Telefoon isku mid ah laba jeer ma muuqdo.
      </p>
      <div className="mt-4 overflow-x-auto rounded-xl border bg-white">
        <table className="w-full text-sm">
          <thead className="border-b bg-slate-800 text-white">
            <tr>
              <th className="p-3 text-left">ID</th>
              <th className="p-3 text-left">Name</th>
              <th className="p-3 text-left">Phone</th>
              <th className="p-3 text-left">Status</th>
              <th className="p-3 text-left">Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.userId} className="border-b hover:bg-slate-50/80">
                <td className="p-3 font-mono text-xs">{u.userId}</td>
                <td className="p-3">{u.fullName}</td>
                <td className="p-3">{u.phone}</td>
                <td className="p-3">
                  <span
                    className={
                      u.status === "active"
                        ? "font-medium text-emerald-600"
                        : "font-medium text-red-600"
                    }
                  >
                    {u.status}
                  </span>
                </td>
                <td className="p-3">
                  <AdminRowActions
                    compact
                    onView={() => openView(u.userId)}
                    onEdit={() => openEdit(u.userId)}
                    onDelete={() => removeUser(u.userId)}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination page={data?.page || 1} totalPages={data?.totalPages || 1} onPageChange={setPage} />

      {modal?.mode === "view" && modal.user && (
        <AdminModal title={`User ${modal.user.userId}`} onClose={() => setModal(null)}>
          <AdminDetailList
            rows={[
              { label: "Name", value: modal.user.fullName },
              { label: "Phone", value: modal.user.phone },
              { label: "Status", value: modal.user.status },
              { label: "Orders", value: String(modal.user.totalOrders) },
              {
                label: "Created",
                value: new Date(modal.user.createdAt).toLocaleString(),
              },
            ]}
          />
        </AdminModal>
      )}

      {modal?.mode === "edit" && modal.user && (
        <AdminModal
          title={`Edit ${modal.user.userId}`}
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
            Status
            <select
              className="mt-1 w-full rounded-lg border px-3 py-2 text-sm"
              value={editForm.status}
              onChange={(e) => setEditForm({ ...editForm, status: e.target.value })}
            >
              <option value="active">active</option>
              <option value="blocked">blocked</option>
            </select>
          </label>
        </AdminModal>
      )}
    </div>
  );
}
