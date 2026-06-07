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

    /// <summary>Email marka order la abuuro (sugitaan lacag). Placeholders: {customerName}, {orderId}, {customerPhone}, {totalAmount}, {trackUrl}, {brandName}, {supportPhone}, {supportEmail}</summary>
    public string OrderCreatedSubject { get; set; } =
        "Dalabkaaga {orderId} — Order ID & lambarka raadinta";

    public string OrderCreatedHtml { get; set; } =
        """
        <p>Salam <strong>{customerName}</strong>,</p>
        <p>Dalabkaaga waa la helay. Fadlan dhammaystir lacag bixinta.</p>
        <p><strong>Order ID:</strong> {orderId}<br/>
        <strong>Telefoonkaaga (raadinta):</strong> {customerPhone}<br/>
        <strong>Lacagta:</strong> <span style="color:#0d9488;">{totalAmount}</span></p>
        <p>Dalabkaaga raadi: <a href="{trackUrl}">{trackUrl}</a><br/>
        Geli Order ID iyo telefoonkaaga bogga raadinta.</p>
        <p>Mahadsanid,<br/><strong>{brandName}</strong></p>
        """;

    /// <summary>Email marka macmiilku dhammeeyo foomka + lacag bixinta. Placeholders: {customerName}, {orderId}, {customerPhone}, {totalAmount}, {trackUrl}, {brandName}, {supportPhone}, {supportEmail}</summary>
    public string OrderSubmittedSubject { get; set; } =
        "Lacag bixinta {orderId} — {totalAmount}";

    public string OrderSubmittedHtml { get; set; } =
        """
        <p>Salam <strong>{customerName}</strong>,</p>
        <p>Waad ku mahadsan tahay — lacag bixintaada waa la helay.</p>
        <p><strong>Order ID:</strong> {orderId}<br/>
        <strong>Telefoonkaaga (raadinta):</strong> {customerPhone}<br/>
        <strong>Lacagta:</strong> <span style="color:#0d9488;">{totalAmount}</span></p>
        <p>Dalabkaaga raadi: <a href="{trackUrl}">{trackUrl}</a></p>
        <p>Mahadsanid,<br/><strong>{brandName}</strong></p>
        """;
}
