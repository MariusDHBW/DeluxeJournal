namespace DeluxeJournal.Task
{
    /// <summary>Flags for enabling smart icons in the task menu.</summary>
    [Flags]
    public enum SmartIconFlags
    {
        /// <summary>Do not show any icons.</summary>
        None = 0,

        /// <summary>Show the item icon.</summary>
        Item = 1 << 0,

        /// <summary>Show building icon.</summary>
        Building = 1 << 2,

        /// <summary>Show the farm animal icon.</summary>
        Animal = 1 << 3,

        /// <summary>Show the NPC icon.</summary>
        Npc = 1 << 4,

        /// <summary>Show the Pet icon.</summary>
        Pet = 1 << 5,

        Shop = 1 << 6,

        Machine = 1 << 7,

        ForageItem = 1 << 8,

        // --- ORTE / EREIGNISSE ---
        Location = 1 << 9,
        FarmLocation = 1 << 10,
        ForageLocation = 1 << 11,
        AnimalLocation = 1 << 12,
        MachineLocation = 1 << 13,
        
        SpecialOrder = 1 << 14,
        PassiveFestival = 1 << 15,
        ActiveFestival = 1 << 16,

        /// <summary>Show all icons.</summary>
        All = ~(-1 << 17)
    }
}
