using System;
namespace Doc.EnumViewModel
{
  class DayOfWeekViewModel
  {
    private readonly DayOfWeek _value;
    public DayOfWeekViewModel(DayOfWeek underlying)
    {
      _value = underlying;
    }
    public bool IsFriday
    {
      get
      {
        return _value == DayOfWeek.Friday;
      }
    }
    public bool IsMonday
    {
      get
      {
        return _value == DayOfWeek.Monday;
      }
    }
    public bool IsSaturday
    {
      get
      {
        return _value == DayOfWeek.Saturday;
      }
    }
    public bool IsSunday
    {
      get
      {
        return _value == DayOfWeek.Sunday;
      }
    }
    public bool IsThursday
    {
      get
      {
        return _value == DayOfWeek.Thursday;
      }
    }
    public bool IsTuesday
    {
      get
      {
        return _value == DayOfWeek.Tuesday;
      }
    }
    public bool IsWednesday
    {
      get
      {
        return _value == DayOfWeek.Wednesday;
      }
    }
  }
}