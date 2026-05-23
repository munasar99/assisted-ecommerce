import { WHATSAPP_GREETING } from "../constants/contact";
import { useWhatsApp } from "../hooks/useWhatsApp";

export default function WhatsAppBand() {
  const { orderId, message, url } = useWhatsApp();

  return (
    <section className="section-pad pt-0">
      <div className="rounded-[2rem] border border-[#25D366]/30 bg-gradient-to-br from-[#dcf8c6]/80 via-white to-brand-50 px-8 py-10 text-center shadow-lg">
        <h2 className="text-xl font-bold text-slate-900">WhatsApp</h2>
        <p className="mx-auto mt-3 max-w-lg text-sm leading-relaxed text-slate-700">{WHATSAPP_GREETING}</p>
        {orderId && (
          <p className="mt-3 font-mono text-sm font-semibold text-brand-800">
            Order ID: <span className="text-brand-600">{orderId}</span>
          </p>
        )}
        <p className="mt-2 text-xs text-slate-500">
          Fariinta WhatsApp waxay ku jiri doontaa qoraalkan
          {orderId ? " iyo Order ID-gaaga" : ""}.
        </p>
        <a
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          className="mt-6 inline-flex items-center gap-2 rounded-full bg-[#25D366] px-8 py-3.5 text-sm font-bold text-white shadow-lg transition hover:bg-[#20bd5a]"
        >
          Fariin WhatsApp
        </a>
        <span className="sr-only">{message}</span>
      </div>
    </section>
  );
}
