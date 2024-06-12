using Microsoft.AspNetCore.Mvc.RazorPages;

public class CallbackModel : PageModel
{
    public required string AccessToken { get; set; }
    public required string ExpiresAt { get; set; }
    public required string MerchantId { get; set; }
    public required string RefreshToken { get; set; }

    public void OnGet(string accessToken, string expiresAt, string merchantId, string refreshToken)
    {
        AccessToken = accessToken;
        ExpiresAt = expiresAt;
        MerchantId = merchantId;
        RefreshToken = refreshToken;
    }
}
