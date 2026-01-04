using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;

namespace DeluxeJournal.Task.Tasks
{
    internal class QueenOfSauceTask : TaskBase
    {
        public string RecipeName { get; set; } = string.Empty;

        public QueenOfSauceTask() : base(TaskTypes.QueenOfSauce) { }

        public QueenOfSauceTask(string name, string recipeName) : base(TaskTypes.QueenOfSauce, name) 
        {
            RecipeName = recipeName;
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

            if (Game1.player.cookingRecipes.ContainsKey(RecipeName))
            {
                Complete = true;
                Game1.playSound("questcomplete"); // <--- Quest-Sound!
            }
        }
    }
}