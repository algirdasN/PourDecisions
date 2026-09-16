using System.Threading.Tasks;

namespace PourDecisions.Desktop.ViewModels;

public interface IAsyncLoadable
{
    Task LoadAsync();
}
