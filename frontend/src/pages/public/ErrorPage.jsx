import { Link } from "react-router-dom";

export default function ErrorPage() {
  return (
    <div className="mx-auto max-w-md px-4 py-20 text-center">
      <h1 className="text-2xl font-bold text-red-600">Khalad</h1>
      <p className="mt-2 text-slate-600">Boggan lama helin ama khalad ayaa dhacay.</p>
      <Link to="/" className="mt-6 inline-block text-brand-600 font-semibold">Home</Link>
    </div>
  );
}
