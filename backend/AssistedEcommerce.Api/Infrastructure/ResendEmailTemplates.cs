namespace AssistedEcommerce.Api.Infrastructure;

/// <summary>
/// Qoraalka email-ka — beddel appsettings (Resend:Templates). Placeholders: {customerName}, {orderId}, {status}, {statusLabel}, {brandName}, {supportPhone}, {supportEmail}
/// </summary>
public class ResendEmailTemplates
{
    public string OrderStatusSubject { get; set; } =
        "Dalabkaaga {orderId} — {statusLabel}";

    /// <summary>HTML gudaha email-ka (qoraalkaaga). Haddii madhan, default code ayaa isticmaala.</summary>
    public string OrderStatusHtml { get; set; } =
        """
        <p>Salam <strong>{customerName}</strong>,</p>
        <p>Asc Welcome Dalabkaaga Si Dhaqso Ah Ayaa laguu Adeygaa.</p>
        <p>Dalabkaaga <strong>{orderId}</strong> wuxuu hadda yahay: <strong style="color:#0d9488;">{statusLabel}</strong></p>
        <p>Haddii su'aal jirto, nala soo xiriir WhatsApp: <strong>{supportPhone}</strong></p>
        <p>Mahadsanid,<br/><strong>{brandName}</strong></p>
        """;

    public string FooterHtml { get; set; } =
        "<p style=\"font-size:12px;color:#94a3b8;margin-top:24px;\">{brandName} · {supportEmail} · {supportPhone}</p>";

    /// <summary>Email marka order la abuuro (sugitaan lacag). Placeholders: {customerName}, {orderId}, {totalAmount}, {brandName}, {supportPhone}, {supportEmail}</summary>
    public string OrderCreatedSubject { get; set; } =
        "Dalabkaaga {orderId} waa la helay — Fadlan bixi lacagta";

    public string OrderCreatedHtml { get; set; } =
        """
        <p>Salam <strong>{customerName}</strong>,</p>
        <p><strong>Asc Welcome Dalabkaaga Si Dhaqso Ah Ayaa laguu Adeygaa.</strong></p>
        <p>Dalabkaaga <strong>{orderId}</strong> waa la keydiyay.</p>
        <p>Wadarta lacagta: <strong style="color:#0d9488;">{totalAmount}</strong></p>
        <p>Fadlan dhammaystir lacag bixinta bogga payment.</p>
        <p>WhatsApp: <strong>{supportPhone}</strong></p>
        <p>Mahadsanid,<br/><strong>{brandName}</strong></p>
        """;

    /// <summary>Email marka macmiilku dhammeeyo foomka + lacag bixinta. Placeholders: {customerName}, {orderId}, {totalAmount}, {brandName}, {supportPhone}, {supportEmail}</summary>
    public string OrderSubmittedSubject { get; set; } =
        "Waad ku mahadsan tahay — Dalabka {orderId} waa la helay";

    public string OrderSubmittedHtml { get; set; } =
        """
        <p>Salam <strong>{customerName}</strong>,</p>
        <p>Asc Welcome! Dalabkaaga iyo lacagtaada waa la helay.</p>
        <p>Dalab: <strong>{orderId}</strong><br/>Wadarta: <strong style="color:#0d9488;">{totalAmount}</strong></p>
        <p>Lacagtaada waa la eegayaa; kadib waa laguu adeegi doonaa si dhaqso ah.</p>
        <p>WhatsApp: <strong>{supportPhone}</strong></p>
        <p>Mahadsanid,<br/><strong>{brandName}</strong></p>
        """;
}
