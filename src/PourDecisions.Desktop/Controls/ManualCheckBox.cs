using Avalonia.Controls;

namespace PourDecisions.Desktop.Controls;

public class ManualCheckBox : CheckBox
{
    protected override Type StyleKeyOverride => typeof(CheckBox);

    protected override void Toggle()
    {
    }
}
