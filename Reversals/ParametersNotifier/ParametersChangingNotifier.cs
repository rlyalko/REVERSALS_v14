using System;
using System.ComponentModel;
using Reversals.Optimization;

namespace Reversals.ParametersNotifier
{
    public class ParametersChangingNotifier : INotifyPropertyChanged
    {        

        public event PropertyChangedEventHandler PropertyChanged;
        private string _parameterName;
        private object _minValue;
        private object _maxValue;
        private object _defaultValue;
        private object _step;
        private OptimizationParameters optParameters;
      
        private void NotifyPropertyChanged(String propertyName = "")
        {
                optParameters.UpdateParameter(_parameterName, _defaultValue, _minValue, _maxValue, _step);
        }

        public string ParameterName
        {
            get { return _parameterName; }
            set
            {
                if (_parameterName != null && value != _parameterName)
                {
                    _parameterName = value;
                    //NotifyPropertyChanged("Name");
                }
            }
        }

        public object MinValue
        {
            get { return _minValue; }
            set
            {
                if (value != _minValue)
                {
                    _minValue = value;
                    //NotifyPropertyChanged("MinValue");
                }
            }
        }

        public object MaxValue
        {
            get { return _maxValue; }
            set
            {
                if (value != _maxValue)
                {
                    _maxValue = value;
                    //NotifyPropertyChanged("MaxValue");
                }
            }
        }

        public object DefaultValue
        {
            get { return _defaultValue; }
            set
            {
                if (value != _defaultValue)
                {
                    _defaultValue = value;
                    //NotifyPropertyChanged("DefaultValue");
                }
            }
        }

        public object Step
        {
            get { return _step; }
            set
            {
                if (value != _step)
                {
                    _step = value;
                    NotifyPropertyChanged("Step");
                }
            }
        }
    }
}
