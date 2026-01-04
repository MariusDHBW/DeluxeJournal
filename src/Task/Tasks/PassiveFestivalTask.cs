using DeluxeJournal.Automation;
using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;
using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class PassiveFestivalTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter(TaskParameterNames.FestivalId, TaskParameterTag.PassiveFestivalId)]
            public string FestivalId { get; set; } = string.Empty;

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.PassiveFestival;

            protected override void InitializeInternal(ITask task)
            {
                if (task is PassiveFestivalTask festTask) FestivalId = festTask.FestivalId;
            }

            protected override ITask? CreateInternal(string name)
            {
                return !string.IsNullOrEmpty(FestivalId) ? new PassiveFestivalTask(name, FestivalId) : null;
            }
        }

        public string FestivalId { get; set; } = string.Empty;

        public PassiveFestivalTask() : base(TaskTypes.PassiveFestival) { }

        public PassiveFestivalTask(string name, string festivalId) : base(TaskTypes.PassiveFestival, name) 
        {
            FestivalId = festivalId;
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
            if (Game1.player.IsInPassiveFestivalLocation(FestivalId))
            {
                Complete = true;
                Game1.playSound("questcomplete");
            }
        }
    }
}