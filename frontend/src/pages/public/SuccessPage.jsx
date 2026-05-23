import { Link } from "react-router-dom";

export default function SuccessPage() {
  return (
    <div className="mx-auto max-w-md px-4 py-20 text-center">
      <div className="rounded-2xl border bg-white p-8 shadow-sm">
        <p className="text-4xl">✓</p>
        <h1 className="mt-4 text-xl font-bold text-emerald-700">Guul!</h1>
        <p className="mt-2 text-slate-600">Waxaad si guul leh u dhammaystirtay.</p>
        <Link to="/track" className="mt-6 inline-block rounded-xl bg-brand-600 px-6 py-2 text-white font-semibold">
          Raadi dalabka
        </Link>
      </div>
    </div>
  );
}
