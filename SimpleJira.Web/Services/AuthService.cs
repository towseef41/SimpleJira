namespace SimpleJira.Web.Services;

public class AuthService
{
    private string? _token;

    public string? Token => _token;

    public void SetToken(string? token)
    {
        _token = token;
    }

    public bool HasToken => !string.IsNullOrWhiteSpace(_token);
}
