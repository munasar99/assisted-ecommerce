import { useEffect, useState } from "react";
import { COMPANY_PAYMENT_NUMBER } from "../../constants/orderStatuses";
import { API_PAGE_LINKS } from "../../constants/apiLinks";
import { CONTACT } from "../../constants/contact";
import { notificationsApi } from "../../api/endpoints";

export default function SettingsPage() {
  const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5298/api";
  const [preview, setPreview] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    notificationsApi
      .preview()
      .then(setPreview)
      .catch(() => setPreview(null))
      .finally(() => setLoading(false));
  }, []);

  const nodeChannels = preview?.nodeJsAi?.channels ?? [];

  return (
    <div>
      <h1 className="text-2xl font-bold">Settings</h1>

      <section className="mt-8">
        <h2 className="text-lg font-semibold text-slate-800">Fariimaha Email &amp; WhatsApp</h2>
        <p className="mt-1 text-sm text-slate-500">
          Halkan waxaad aragtaa <strong>cidda</strong> fariinta loo diro iyo <strong>goorta</strong>.
        </p>

        {loading ? (
          <p className="mt-4 text-sm text-slate-400">Soo raraya...</p>
        ) : (
          <div className="mt-4 space-y-6">
            <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
              <strong>Xeerka:</strong> Node.js AI — fariin kaliya marka <strong>Payment Submit</strong>. Order submit → kaydi kaliya.
            </div>

            <div>
              <h3 className="font-medium text-slate-700">ASP.NET — Resend Email (System Weyn)</h3>
              {preview?.aspNetResend ? (
                <div className="mt-2 grid gap-3 md:grid-cols-2">
                  <InfoCard title="Goorta" rows={preview.aspNetResend.when?.map((w) => ({ label: "•", value: w }))} />
                  <InfoCard
                    title="Cidda loo diraa"
                    rows={[
                      { label: "Macmiil", value: preview.aspNetResend.sentTo },
                      { label: "Ka soo baxa", value: `${preview.aspNetResend.fromName} <${preview.aspNetResend.fromEmail}>` },
                      { label: "Support", value: `${preview.aspNetResend.supportPhone} · ${preview.aspNetResend.supportEmail}` },
                      { label: "Configured", value: preview.aspNetResend.configured ? "Haa ✓" : "Maya ✗" },
                    ]}
                  />
                </div>
              ) : (
                <p className="mt-2 text-sm text-slate-400">Resend preview lama helin</p>
              )}
            </div>

            <div>
              <h3 className="font-medium text-slate-700">Node.js AI — Payment kadib (4 fariin)</h3>
              {nodeChannels.length === 0 ? (
                <p className="mt-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                  Node.js ma socdo ama preview lama helin. Shid: <code className="font-mono">Ecommerce-Ai/backend → npm run dev</code>
                </p>
              ) : (
                <div className="mt-3 grid gap-4 lg:grid-cols-2">
                  {nodeChannels.map((ch) => (
                    <div key={ch.id} className="rounded-xl border bg-white p-4 shadow-sm">
                      <div className="flex items-center justify-between gap-2">
                        <h4 className="font-semibold text-slate-800">{ch.label}</h4>
                        <span
                          className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                            ch.configured ? "bg-emerald-100 text-emerald-800" : "bg-slate-100 text-slate-500"
                          }`}
                        >
                          {ch.configured ? "Configured" : "Ma configured"}
                        </span>
                      </div>
                      <dl className="mt-3 space-y-2 text-sm">
                        <Row label="Goorta" value={ch.when} />
                        <Row label="Loo diraa" value={ch.sentTo} />
                        <Row label="Tusaale" value={ch.sentToExample} mono />
                        <Row label="Ka soo baxa" value={ch.from} />
                        {ch.skipIf && <Row label="La iska dhaafaa haddii" value={ch.skipIf} />}
                      </dl>
                      <pre className="mt-3 max-h-40 overflow-auto rounded-lg bg-slate-50 p-3 text-xs whitespace-pre-wrap text-slate-600">
                        {ch.bodyPreview}
                      </pre>
                    </div>
                  ))}
                </div>
              )}
              {preview?.nodeJsAi?.config && (
                <p className="mt-3 text-xs text-slate-400">
                  Node config: Admin email {preview.nodeJsAi.config.adminEmail} · Admin WhatsApp{" "}
                  {preview.nodeJsAi.config.adminWhatsapp}
                </p>
              )}
            </div>

            <div className="rounded-xl border bg-white p-4">
              <h3 className="font-medium text-slate-700">WhatsApp bandhig (frontend — macmiilku wuu riixaa)</h3>
              <p className="mt-2 text-sm text-slate-600">
                Tani ma aha fariin otomaatig — macmiilku wuxuu la xiriiraa ganacsiga:
              </p>
              <dl className="mt-2 space-y-1 text-sm">
                <Row label="Number" value={CONTACT.phoneDisplay} />
                <Row label="wa.me" value={CONTACT.whatsappWaMe} mono />
                <Row label="Email contact" value={CONTACT.email} />
              </dl>
            </div>
          </div>
        )}
      </section>

      <div className="mt-10 max-w-lg space-y-4 rounded-xl border bg-white p-6">
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
      </div>

      <section className="mt-10">
        <h2 className="text-lg font-semibold">Bog ↔ API (xiriirka)</h2>
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

function Row({ label, value, mono }) {
  return (
    <div className="flex gap-2">
      <dt className="shrink-0 font-medium text-slate-500">{label}:</dt>
      <dd className={`text-slate-700 ${mono ? "font-mono text-xs" : ""}`}>{value}</dd>
    </div>
  );
}

function InfoCard({ title, rows }) {
  return (
    <div className="rounded-xl border bg-white p-4">
      <h4 className="font-medium text-slate-800">{title}</h4>
      <dl className="mt-2 space-y-1 text-sm">
        {rows?.map((r, i) => (
          <Row key={i} label={r.label} value={r.value} />
        ))}
      </dl>
    </div>
  );
}
