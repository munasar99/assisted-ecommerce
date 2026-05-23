import { Link } from "react-router-dom";
import { BRAND } from "../constants/brand";
import { CONTACT } from "../constants/contact";
import { useWhatsApp } from "../hooks/useWhatsApp";
import Logo from "./Logo";

export default function Footer() {
  const { url: whatsappUrl } = useWhatsApp();

  return (
    <footer className="mt-auto border-t border-slate-200 bg-slate-900 text-slate-300">
      <div className="mx-auto max-w-6xl px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid gap-10 md:grid-cols-3">
          <div>
            <Logo light />
            <p className="mt-4 max-w-xs text-sm leading-relaxed text-slate-400">
              {BRAND.taglineShort} — invoice, lacag bixin, iyo 18 degmo Muqdisho.
            </p>
          </div>
          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-white">Macmiil</h4>
            <ul className="mt-4 space-y-2 text-sm">
              <li>
                <Link to="/order" className="hover:text-white">
                  Dalbo
                </Link>
              </li>
              <li>
                <Link to="/payment" className="hover:text-white">
                  Lacag bixi
                </Link>
              </li>
              <li>
                <Link to="/track" className="hover:text-white">
                  Dalabkayga
                </Link>
              </li>
            </ul>
          </div>
          <div>
            <h4 className="text-xs font-semibold uppercase tracking-wider text-white">Xiriir</h4>
            <ul className="mt-4 space-y-2 text-sm text-slate-400">
              <li>{CONTACT.city}</li>
              <li>
                <a
                  href={whatsappUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-2 font-medium text-[#25D366] hover:text-[#5ae87a]"
                >
                  WhatsApp — nagala soo xiriir
                </a>
              </li>
              <li>
                <a href={`mailto:${CONTACT.email}`} className="hover:text-white">
                  {CONTACT.email}
                </a>
              </li>
            </ul>
          </div>
        </div>
        <div className="mt-10 flex flex-col items-center justify-between gap-2 border-t border-slate-800 pt-8 text-xs text-slate-500 sm:flex-row">
          <p>
            © {new Date().getFullYear()} {BRAND.name}. Dhammaan xuquuqda way dhawran yihiin.
          </p>
          <p className="text-slate-500">{BRAND.taglineShort}</p>
        </div>
      </div>
    </footer>
  );
}
