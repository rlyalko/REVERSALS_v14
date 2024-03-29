﻿using System;

namespace Reversals.Optimization
{
    class NotOptimizationParameter
    {
        public string Indicator;
        public string Name;
        public Type Type;
        public object MinValue;
        public object MaxValue;
        public object DefaultValue;
        public object Step;

        public NotOptimizationParameter(string indicator, string name, Type type, object min, object max, object value, object step)
        {
            Indicator = indicator;
            Name = name;
            Type = type;
            MinValue = min;
            MaxValue = max;
            DefaultValue = value;
            Step = step;
        }
    }
}
