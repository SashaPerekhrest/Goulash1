import type { Metadata } from "next";
import { Footer } from "../components/Footer";
import { Header } from "../components/Header";
import "../styles/globals.css";

export const metadata: Metadata = {
  title: {
    default: "Портфолио",
    template: "%s | Портфолио"
  },
  description: "Публичное портфолио специалиста по работе и внедрению ИИ"
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ru">
      <body>
        <Header />
        {children}
        <Footer />
      </body>
    </html>
  );
}
