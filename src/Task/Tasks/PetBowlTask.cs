using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;

namespace DeluxeJournal.Task.Tasks
{
    internal class PetBowlTask : TaskBase
    {
        public PetBowlTask() : base(TaskTypes.PetBowl) { }

        public PetBowlTask(string name) : base(TaskTypes.PetBowl, name) { }

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

            var bowls = Game1.getFarm().buildings.OfType<PetBowl>();
            bool allWatered = true;

            if (!bowls.Any()) return; 

            foreach (var bowl in bowls)
            {
                if (!bowl.watered.Value)
                {
                    allWatered = false;
                    break;
                }
            }

            if (allWatered)
            {
                if (!Complete)
                {
                    Complete = true;
                    Game1.playSound("questcomplete");
                }
            }
            else if (Complete)
            {
                Complete = false;
            }
        }
    }
}