export default function PageShell({ title, subtitle, children, maxWidth = "max-w-2xl", centered = false }) {
  return (
    <div className={`mx-auto w-full px-4 py-10 sm:px-6 lg:px-8 ${maxWidth}`}>
      {(title || subtitle) && (
        <header className={`mb-8 ${centered ? "text-center" : ""}`}>
          {title && (
            <h1 className="text-2xl font-bold tracking-tight text-brand-800 sm:text-3xl">{title}</h1>
          )}
          {subtitle && (
            <p className="mt-2 text-sm leading-relaxed text-slate-600 sm:text-base">{subtitle}</p>
          )}
        </header>
      )}
      {children}
    </div>
  );
}
