﻿using System;

namespace OpenCBS.Engine.Interfaces
{
    public interface IPeriodPolicy : IPolicy
    {
        DateTime GetNextDate(DateTime date);
        DateTime GetPreviousDate(DateTime date);
        int GetNumberOfDays(DateTime date);
        int GetNumberOfPeriodsInYear(DateTime date, IYearPolicy yearPolicy);
    }
}
