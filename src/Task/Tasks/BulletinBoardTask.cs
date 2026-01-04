using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;

namespace DeluxeJournal.Task.Tasks
{
    internal class BulletinBoardTask : TaskBase
    {
        public BulletinBoardTask() : base(TaskTypes.BulletinBoard) { }

        public BulletinBoardTask(string name) : base(TaskTypes.BulletinBoard, name) { }

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

            if (Game1.player.acceptedDailyQuest.Value)
            {
                Complete = true;
                Game1.playSound("questcomplete");
            }
        }
    }
}