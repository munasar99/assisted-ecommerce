import { useEffect, useRef, useState } from "react";
import { Link, Navigate, useNavigate, useSearchParams } from "react-router-dom";
import Logo from "../../components/Logo";
import { useAuth } from "../../context/AuthContext";
import { useToast } from "../../context/ToastContext";

export default function AdminLoginPage() {
  const { login, isAuthenticated, ready } = useAuth();
  const navigate = useNavigate();
  const { show } = useToast();
  const [searchParams] = useSearchParams();
  const sessionExpired = searchParams.get("expired") === "1";
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [fieldsReady, setFieldsReady] = useState(false);
  const formKey = useRef(`admin-login-${Date.now()}`);

  useEffect(() => {
    setEmail("");
    setPassword("");
    const t = requestAnimationFrame(() => setFieldsReady(true));
    return () => {
      cancelAnimationFrame(t);
      setEmail("");
      setPassword("");
    };
  }, []);

  useEffect(() => {
    if (sessionExpired) show("Session-ka waa dhacay. Mar kale gal.", "error");
  }, [sessionExpired, show]);

  if (!ready) return null;
  if (isAuthenticated) return <Navigate to="/admin/dashboard" replace />;

  const submit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await login(email.trim(), password);
      setPassword("");
      show("Login successful", "success");
      navigate("/admin/dashboard");
    } catch (err) {
      show(err.message, "error");
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-100 px-4 py-12">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-brand-100/80 via-slate-100 to-slate-200" aria-hidden />

      <article className="relative w-full max-w-[420px]">
        <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-2xl shadow-slate-300/40">
          <div className="h-1.5 bg-gradient-to-r from-brand-500 via-brand-600 to-brand-700" />

          <div className="p-8 sm:p-10">
            <header>
              <p className="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">
                Admin Panel
              </p>
              <Logo />
            </header>

            <h1 className="mt-8 text-2xl font-bold tracking-tight text-slate-900">Sign in</h1>
            <p className="mt-1 text-sm text-slate-500">Geli email iyo password-kaaga adigu</p>

            <form
              key={formKey.current}
              onSubmit={submit}
              className="relative mt-8 space-y-5"
              autoComplete="off"
              data-lpignore="true"
              data-1p-ignore
            >
              {/* Decoy fields — browser autofill waxay halkan ku dhacdaa */}
              <div className="absolute -left-[9999px] h-0 w-0 overflow-hidden" aria-hidden tabIndex={-1}>
                <input type="text" name="fake_user" autoComplete="username" tabIndex={-1} />
                <input type="password" name="fake_pass" autoComplete="current-password" tabIndex={-1} />
              </div>

              <label className="block">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Email</span>
                <input
                  className="input-field"
                  type="text"
                  inputMode="email"
                  name="admin_email_field"
                  id="admin-email-input"
                  autoComplete="off"
                  autoCorrect="off"
                  autoCapitalize="off"
                  spellCheck={false}
                  data-form-type="other"
                  placeholder="Enter your email"
                  value={email}
                  readOnly={!fieldsReady}
                  onFocus={(e) => e.target.removeAttribute("readonly")}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </label>
              <label className="block">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Password</span>
                <input
                  className="input-field"
                  type="password"
                  name="admin_secret_field"
                  id="admin-password-input"
                  autoComplete="off"
                  data-form-type="other"
                  placeholder="Enter your password"
                  value={password}
                  readOnly={!fieldsReady}
                  onFocus={(e) => e.target.removeAttribute("readonly")}
                  onChange={(e) => setPassword(e.target.value)}
                />
              </label>
              <button type="submit" disabled={loading} className="btn-primary w-full !py-3">
                {loading ? "Signing in…" : "Enter Dashboard"}
              </button>
            </form>
          </div>
        </div>

        <p className="mt-6 text-center text-sm text-slate-500">
          <Link to="/home" className="font-medium text-brand-700 hover:underline">
            ← Back to website
          </Link>
        </p>
      </article>
    </section>
  );
}
