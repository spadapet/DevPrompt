using System;
using System.Globalization;
using System.Windows;

namespace DevPrompt.Update.Utility
{
    internal sealed class StageToProgressBarVisibilityConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            WorkerStageType stageType = WorkerStageType.Done;
            bool invert = parameter is bool boolParameter && boolParameter;

            if (value is WorkerStage stage)
            {
                stageType = stage.Stage;
            }
            else if (value is WorkerStageType valueAsStageType)
            {
                stageType = valueAsStageType;
            }

            return (stageType == WorkerStageType.Done)
                ? (invert ? Visibility.Visible : Visibility.Collapsed)
                : (invert ? Visibility.Collapsed : Visibility.Visible);
        }
    }
}
