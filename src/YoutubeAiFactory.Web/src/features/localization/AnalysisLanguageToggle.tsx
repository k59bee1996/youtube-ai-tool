export type AnalysisLocale = "en" | "vi";

export function AnalysisLanguageToggle({ locale, onChange, disabled }: { locale: AnalysisLocale; onChange: (locale: AnalysisLocale) => void; disabled?: boolean }) {
  return (
    <div className="analysis-language-toggle" role="group" aria-label="Analysis language">
      <button className={locale === "en" ? "is-selected" : ""} type="button" onClick={() => onChange("en")} disabled={disabled}>EN</button>
      <button className={locale === "vi" ? "is-selected" : ""} type="button" onClick={() => onChange("vi")} disabled={disabled}>VI</button>
    </div>
  );
}
