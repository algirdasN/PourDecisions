using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.Desktop.ViewModels;

public partial class BottleViewModel(Bottle bottle) : ViewModelBase
{
    private static readonly FillLevel[] FillLevels;
    private static readonly Dictionary<FillLevel, string> FillLabels;

    private readonly int _id = bottle.Id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FillDisplay))]
    private FillLevel _fill = bottle.FillLevel;

    static BottleViewModel()
    {
        FillLevels = Enum.GetValues<FillLevel>();
        FillLabels = new Dictionary<FillLevel, string>();

        var label = new char[FillLevels.Length];
        Array.Fill(label, '█');

        for (var i = 0; i < FillLevels.Length; i++)
        {
            FillLabels[FillLevels[i]] = new string(label);
            label[i] = '░';
        }
    }

    public string Name { get; } = bottle.Name;
    public string Volume { get; } = $"{bottle.Volume} ml";
    public string FillDisplay => FillLabels[Fill];

    public event Action<int, FillLevel>? FillLevelChanged;
    public event Action<int, string>? DeleteBottleClicked;

    [RelayCommand]
    private void CycleFillLevel()
    {
        var nextIndex = (FillLevels.IndexOf(Fill) + 1) % FillLevels.Length;
        var nextFill = FillLevels[nextIndex];
        FillLevelChanged?.Invoke(_id, nextFill);
        Fill = nextFill;
    }

    [RelayCommand]
    private void DeleteBottle()
    {
        DeleteBottleClicked?.Invoke(_id, Name);
    }
}
