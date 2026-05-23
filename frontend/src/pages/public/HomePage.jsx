import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { BRAND } from "../../constants/brand";
import WhatsAppBand from "../../components/WhatsAppBand";
import { useWhatsApp } from "../../hooks/useWhatsApp";

const steps = [
  {
    num: "01",
    title: "Dalbo alaabta",
    desc: "Geli link Alibaba/Amazon, dooro Home delivery ama Tabo, 18 degmo.",
    card: "feature-card feature-card-teal",
  },
  {
    num: "02",
    title: "Bixi & xaqiiji",
    desc: "Hel invoice, soo dir screenshot lacag bixinta — si ammaan ah.",
    card: "feature-card feature-card-sky",
  },
  {
    num: "03",
    title: "Hel Muqdisho",
    desc: "Raadi dalabkaaga telefoon + order ID ilaa alaabtu gurigaaga timaado.",
    card: "feature-card feature-card-warm",
  },
];

const perks = [
  { label: "18 degmo", sub: "Muqdisho", color: "text-brand-600" },
  { label: "Invoice", sub: "Automatic", color: "text-ocean-600" },
  { label: "Tabo", sub: "Delivery $0", color: "text-accent-600" },
];

const platforms = ["Alibaba", "Amazon", "AliExpress", "Dukaamo kale"];

const paymentMethods = [
  { name: "EVC Plus", sub: "Hormuud" },
  { name: "Zaad", sub: "Telesom" },
  { name: "Sahal", sub: "Golis" },
  { name: "Screenshot", sub: "Upload kadib invoice" },
];

const faqs = [
  {
    q: "Sidee baan u dalbaa alaab Alibaba ama Amazon?",
    a: "Guji Bilow Dalabka, ku dheji link-ga product-ka, buuxi magacaaga iyo telefoonka, dooro delivery, kadib submit.",
  },
  {
    q: "Waa maxay farqiga Home delivery iyo Tabo?",
    a: "Home delivery — alaabtu waxay kuu imaanaysaa 18 degmo (fee degmo kasta). Tabo — waxaad soo qaadaneysaa xafiiska, delivery $0.",
  },
  {
    q: "Sidee baan u bixiyaa lacagta?",
    a: "Marka invoice la soo diro, ku bixi EVC/Zaad/Sahal, kadib soo dir screenshot bogga Lacag bixin ama WhatsApp.",
  },
  {
    q: "Sidee baan u raadiyaa dalabkayga?",
    a: "Bogga Raadi — geli Order ID + telefoonka aad dalbatay.",
  },
  {
    q: "Ma kula hadli karaa WhatsApp?",
    a: "Haa — guji badhanka cagaaran ee hoose ama Fariin WhatsApp.",
  },
];

const PREFILL_KEY = "prefillProductUrl";

export default function HomePage() {
  const navigate = useNavigate();
  const { url: whatsappPayUrl } = useWhatsApp("Waxaan rabaa inaan soo diro screenshot lacag bixinta.");
  const [productLink, setProductLink] = useState("");

  const startOrder = () => {
    const url = productLink.trim();
    if (url) sessionStorage.setItem(PREFILL_KEY, url);
    navigate("/order");
  };

  return (
    <>
      {/* Hero */}
      <section className="section-pad pb-8 pt-6 lg:pt-10">
        <div className="relative overflow-hidden rounded-[2rem] bg-gradient-to-br from-brand-900 via-brand-800 to-slate-900 px-6 py-14 text-white shadow-2xl shadow-brand-900/30 sm:px-12 sm:py-20">
          <div
            className="pointer-events-none absolute inset-0 opacity-40"
            style={{
              backgroundImage:
                "radial-gradient(circle at 20% 20%, rgba(52,211,153,0.35), transparent 45%), radial-gradient(circle at 80% 10%, rgba(14,165,233,0.25), transparent 40%), radial-gradient(circle at 70% 90%, rgba(249,115,22,0.15), transparent 45%)",
            }}
            aria-hidden
          />
          <div className="pointer-events-none absolute -right-24 -top-24 h-72 w-72 rounded-full bg-white/10 blur-3xl" />
          <div className="relative z-10 max-w-2xl">
            <p className="section-label !bg-white/15 !text-emerald-100 !ring-white/20">
              Ku soo dhawoow {BRAND.name}
            </p>
            <h1 className="mt-5 font-display text-3xl font-bold leading-[1.15] tracking-tight sm:text-5xl">
              Dalbo Alibaba & Amazon —{" "}
              <span className="bg-gradient-to-r from-emerald-200 to-teal-100 bg-clip-text text-transparent">
                Keena Muqdisho
              </span>
            </h1>
            <p className="mt-5 text-base leading-relaxed text-emerald-100/90 sm:text-lg">
              Adeeg fudud: dalab, invoice, lacag bixin, iyo raadraac — dhammaan hal meel.
            </p>
            <div className="mt-6 flex flex-wrap gap-2">
              {platforms.map((p) => (
                <span
                  key={p}
                  className="rounded-full border border-white/25 bg-white/10 px-3 py-1 text-xs font-semibold text-white/90"
                >
                  {p}
                </span>
              ))}
            </div>
            <div className="mt-6 flex flex-wrap gap-3">
              {perks.map((p) => (
                <span key={p.label} className="trust-pill">
                  <span className={`font-bold ${p.color}`}>{p.label}</span>
                  <span className="text-white/70">{p.sub}</span>
                </span>
              ))}
            </div>

            {/* Quick paste link */}
            <div className="mt-8 rounded-2xl border border-white/20 bg-white/10 p-4 backdrop-blur-sm">
              <p className="text-xs font-semibold uppercase tracking-wider text-emerald-100">
                Degdeg — ku dheji link product
              </p>
              <div className="mt-3 flex flex-col gap-2 sm:flex-row">
                <input
                  type="url"
                  value={productLink}
                  onChange={(e) => setProductLink(e.target.value)}
                  placeholder="https://www.amazon.com/... ama alibaba.com/..."
                  className="flex-1 rounded-xl border border-white/20 bg-white/95 px-4 py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-300"
                />
                <button
                  type="button"
                  onClick={startOrder}
                  className="rounded-xl bg-white px-6 py-3 text-sm font-bold text-brand-800 shadow-md transition hover:bg-brand-50"
                >
                  Sii wad →
                </button>
              </div>
            </div>

            <div className="mt-8 flex flex-wrap gap-4">
              <Link
                to="/order"
                className="inline-flex items-center gap-2 rounded-full bg-white px-7 py-3.5 text-sm font-bold text-brand-800 shadow-lg shadow-black/10 transition hover:bg-brand-50"
              >
                Bilow Dalabka
                <span aria-hidden>→</span>
              </Link>
              <Link
                to="/track"
                className="inline-flex items-center gap-2 rounded-full border-2 border-white/35 px-7 py-3.5 text-sm font-semibold backdrop-blur-sm transition hover:border-white/60 hover:bg-white/10"
              >
                Raadi Dalabka
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="section-mint section-pad">
        <p className="section-label">Hab fudud</p>
        <h2 className="section-title mt-4">Saddex talaabo oo keliya</h2>
        <p className="section-subtitle">
          Ma aha mid adag — macmiil ahaan waxaad sameysaa dalab, bixisaa, oo raadraacdaa.
        </p>
        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {steps.map((s) => (
            <article key={s.num} className={s.card}>
              <span className="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-sm font-bold text-white shadow-md">
                {s.num}
              </span>
              <span className="mt-4 block text-xs font-bold uppercase tracking-widest text-slate-400">
                Talaabo {s.num}
              </span>
              <h3 className="mt-2 text-xl font-bold text-slate-900">{s.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-slate-600">{s.desc}</p>
            </article>
          ))}
        </div>
      </section>

      {/* Payment */}
      <section className="section-sky section-pad">
        <p className="section-label !text-ocean-700 !ring-ocean-200/60">Lacag bixin</p>
        <h2 className="section-title mt-4">Invoice kadib — soo dir screenshot</h2>
        <p className="section-subtitle">
          Marka invoice la soo diro, ku bixi mid ka mid ah hababkan, kadib upload screenshot bogga
          lacag bixinta.
        </p>
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {paymentMethods.map((m) => (
            <div key={m.name} className="feature-card text-center">
              <p className="text-lg font-bold text-slate-900">{m.name}</p>
              <p className="mt-1 text-xs text-slate-500">{m.sub}</p>
            </div>
          ))}
        </div>
        <div className="mt-6 flex flex-wrap gap-3">
          <Link to="/track" className="btn-primary">
            Soo dir lacag bixinta
          </Link>
          <a
            href={whatsappPayUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 rounded-xl bg-[#25D366] px-5 py-2.5 text-sm font-semibold text-white shadow-md transition hover:bg-[#20bd5a]"
          >
            WhatsApp screenshot
          </a>
        </div>
      </section>

      {/* FAQ */}
      <section className="section-mint section-pad">
        <p className="section-label">Su'aalo</p>
        <h2 className="section-title mt-4">FAQ</h2>
        <div className="mt-8 space-y-3">
          {faqs.map((f) => (
            <details key={f.q} className="faq-item group">
              <summary className="flex items-center justify-between gap-4">
                {f.q}
                <span className="text-brand-600 transition group-open:rotate-45">+</span>
              </summary>
              <p className="mt-3 text-sm leading-relaxed text-slate-600">{f.a}</p>
            </details>
          ))}
        </div>
      </section>

      <WhatsAppBand />

      {/* CTA warm */}
      <section className="section-cream section-pad pt-0">
        <div className="rounded-[2rem] border border-accent-100 bg-gradient-to-r from-accent-50 via-white to-brand-50 px-8 py-12 text-center shadow-lg shadow-accent-100/40 sm:px-14">
          <h2 className="section-title">Diyaar ma u tahay inaad dalbato?</h2>
          <p className="section-subtitle mx-auto">
            Ku bilow dalab cusub — waxaan ku caawinaynaa Alibaba, Amazon, iyo dukaamada kale.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <Link to="/order" className="btn-primary">
              Bilow Dalab Cusub
            </Link>
            <Link to="/track" className="btn-secondary">
              Soo dir lacag bixinta
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}
