import { COMPANY_PAYMENT_NUMBER } from "../../constants/orderStatuses";
import { API_PAGE_LINKS } from "../../constants/apiLinks";

export default function SettingsPage() {
  const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5298/api";

  return (
    <div>
      <h1 className="text-2xl font-bold">Settings</h1>
      <div className="mt-6 max-w-lg space-y-4 rounded-xl border bg-white p-6">
        <div>
          <label className="text-sm font-medium text-slate-700">Payment number (display)</label>
          <p className="mt-1 rounded-lg bg-slate-50 px-3 py-2 font-mono">{COMPANY_PAYMENT_NUMBER}</p>
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700">Database</label>
          <p className="mt-1 text-slate-600">ubaxsana (MongoDB)</p>
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700">API URL</label>
          <p className="mt-1 font-mono text-sm text-slate-600">{apiUrl}</p>
        </div>
        <p className="text-xs text-slate-400">Edit appsettings.json (backend) for production secrets.</p>
      </div>

      <section className="mt-10">
        <h2 className="text-lg font-semibold">Bog ↔ API (xiriirka)</h2>
        <p className="mt-1 text-sm text-slate-500">
          Bog kasta wuxuu u yeedhaa endpoint-ka hoose. Tijaabi: Swagger{" "}
          <a href={apiUrl.replace(/\/api\/?$/i, "/swagger")} className="text-brand-600 underline" target="_blank" rel="noreferrer">
            /swagger
          </a>
        </p>
        <div className="mt-4 overflow-x-auto rounded-xl border bg-white">
          <table className="w-full text-left text-sm">
            <thead className="border-b bg-slate-50">
              <tr>
                <th className="p-3">Page</th>
                <th className="p-3">Method</th>
                <th className="p-3">API path</th>
                <th className="p-3">Auth</th>
              </tr>
            </thead>
            <tbody>
              {API_PAGE_LINKS.map((row, i) => (
                <tr key={`${row.page}-${row.path}-${i}`} className="border-b">
                  <td className="p-3 font-mono text-xs">{row.page}</td>
                  <td className="p-3">{row.method}</td>
                  <td className="p-3 font-mono text-xs">{row.path}</td>
                  <td className="p-3">{row.auth ? "Admin JWT" : "Public"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
