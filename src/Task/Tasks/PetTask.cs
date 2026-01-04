using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class PetTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter(TaskParameterNames.Pet, TaskParameterTag.PetName)]
            public string? PetName { get; set; }

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.Pet;
            protected override void InitializeInternal(ITask task)
            {
                if (task is PetTask petTask)
                {
                    PetName = petTask.PetName;
                }
            }

            protected override ITask? CreateInternal(string name)
            {
                return !string.IsNullOrEmpty(PetName) ? new PetTask(name, PetName) : null;
            }
        }

        public string PetName { get; set; } = string.Empty;

        public PetTask() : base(TaskTypes.Pet) { }

        public PetTask(string name, string petName) : base(TaskTypes.Pet, name) 
        {
            PetName = petName;
        }

        public override void EventSubscribe(ITaskEvents events)
        {
            if (DeluxeJournalMod.Instance?.Helper != null)
                DeluxeJournalMod.Instance.Helper.Events.GameLoop.OneSecondUpdateTicked += OnUpdate;
        }

        public override void EventUnsubscribe(ITaskEvents events)
        {
            if (DeluxeJournalMod.Instance?.Helper != null)
                DeluxeJournalMod.Instance.Helper.Events.GameLoop.OneSecondUpdateTicked -= OnUpdate;
        }

        private void OnUpdate(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!CanUpdate()) return;

            bool ignoreMaxed = DeluxeJournalMod.Config?.IgnoreMaxedPets ?? false;

            Utility.ForEachCharacter((npc) =>
            {
                if (npc is Pet pet && pet.displayName == PetName)
                {
                    bool pettedToday = false;
                    if (pet.lastPetDay.TryGetValue(Game1.player.UniqueMultiplayerID, out int dayPetted))
                    {
                        if (dayPetted == Game1.Date.TotalDays) pettedToday = true;
                    }

                    if (pettedToday || (ignoreMaxed && pet.friendshipTowardFarmer.Value >= 1000))
                    {
                        Complete = true;
                        Game1.playSound("questcomplete");
                    }
                    return false;
                }
                return true;
            });
        }
    }
}