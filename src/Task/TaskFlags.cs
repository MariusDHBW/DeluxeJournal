namespace DeluxeJournal.Task
{
    // --- JAHRESZEITEN ---
    [Flags]
    public enum SeasonFlags
    {
        None = 0,
        Spring = 1,
        Summer = 2,
        Fall = 4,
        Winter = 8,
        All = Spring | Summer | Fall | Winter
    }

    // --- WOCHENTAGE ---
    [Flags]
    public enum WeekdayFlags
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,
        All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
    }

    // --- WOCHEN DES MONATS ---
    [Flags]
    public enum WeekFlags
    {
        None = 0,
        Week1 = 1,
        Week2 = 2,
        Week3 = 4,
        Week4 = 8,
        All = Week1 | Week2 | Week3 | Week4
    }

    // --- WETTER ---
    [Flags]
    public enum WeatherFlags
    {
        None = 0,
        Sun = 1,
        Rain = 2,
        All = Sun | Rain
    }
}