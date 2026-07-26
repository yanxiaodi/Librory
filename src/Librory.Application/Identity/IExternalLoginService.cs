namespace Librory.Application.Identity;

public interface IExternalLoginService
{
    Task<ExternalLoginResult> SignInAsync(ExternalLoginRequest request, CancellationToken cancellationToken);
}
