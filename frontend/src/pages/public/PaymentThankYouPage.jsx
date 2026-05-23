import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import PageShell from "../../components/PageShell";

export default function PaymentThankYouPage() {
  const location = useLocation();
  const [emailSent] = useState(
    () =>
      location.state?.emailSent === true ||
      sessionStorage.getItem("paymentEmailSent") === "1",
  );

  useEffect(() => {
    sessionStorage.removeItem("paymentEmailSent");
  }, []);

  return (
    <PageShell title="Waad Mahadsantahay" centered maxWidth="max-w-md">
      <div className="card-elevated space-y-5 p-10 text-center sm:p-12">
        <p className="text-5xl" aria-hidden>
          ✓
        </p>
        <h2 className="text-2xl font-bold text-emerald-700 sm:text-3xl">
          Waad Mahadsantahay
        </h2>
        <p className="text-sm leading-relaxed text-slate-600 sm:text-base">
          Lacagtaada waa la helay. Admin wuu xaqiijin doonaa lacagtaada kadibna
          wuu bilaabi doonaa diyaarinta dalabkaaga.
        </p>
        {emailSent && (
          <p className="text-sm font-medium text-emerald-700">
            Email xaqiijin ah ayaa loo diray cinwaanka aad form-ka ku gelisay.
          </p>
        )}
      </div>
    </PageShell>
  );
}
