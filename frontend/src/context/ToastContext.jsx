import { createContext, useCallback, useContext, useState } from "react";

const ToastContext = createContext(null);

export function ToastProvider({ children }) {
  const [toast, setToast] = useState(null);

  const show = useCallback((message, type = "info") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  }, []);

  return (
    <ToastContext.Provider value={{ show }}>
      {children}
      {toast && (
        <div className="pointer-events-none fixed inset-0 z-[60] flex items-center justify-center px-4">
          <div
            className={`pointer-events-auto max-w-md rounded-2xl px-6 py-4 text-center text-base font-semibold leading-snug shadow-2xl text-white ${
              toast.type === "error"
                ? "bg-red-600"
                : toast.type === "success"
                  ? "bg-emerald-600"
                  : "bg-slate-800"
            }`}
            role="status"
          >
            {toast.message}
          </div>
        </div>
      )}
    </ToastContext.Provider>
  );
}

export const useToast = () => useContext(ToastContext);
