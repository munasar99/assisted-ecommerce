import { Outlet } from "react-router-dom";
import Navbar from "../components/Navbar";
import Footer from "../components/Footer";
import WhatsAppFloat from "../components/WhatsAppFloat";

export default function PublicLayout() {
  return (
    <div className="page-bg flex min-h-screen flex-col">
      <Navbar />
      <main className="flex-1 pb-24">
        <Outlet />
      </main>
      <Footer />
      <WhatsAppFloat />
    </div>
  );
}
