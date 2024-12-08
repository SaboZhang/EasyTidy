using System.Threading.Tasks;

namespace EasyTidy.Contracts.Service;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
