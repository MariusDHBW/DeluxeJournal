using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;
using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class ActiveFestivalTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter(TaskParameterNames.FestivalId, TaskParameterTag.ActiveFestivalId)]
            public string FestivalName { get; set; } = string.Empty;

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.ActiveFestival;

            protected override void InitializeInternal(ITask task)
            {
                if (task is ActiveFestivalTask activeTask)
                {
                    FestivalName = activeTask.TargetFestivalName;
                }
            }

            protected override ITask? CreateInternal(string name)
            {
                return new ActiveFestivalTask(name, FestivalName);
            }
        }

        /// <summary>Der Name des Festivals (z.B. "Egg Festival"). Wenn leer, zählt jedes Festival.</summary>
        public string TargetFestivalName { get; set; } = string.Empty;

        public ActiveFestivalTask() : base(TaskTypes.ActiveFestival) { }

        public ActiveFestivalTask(string name, string festivalName) : base(TaskTypes.ActiveFestival, name) 
        {
            TargetFestivalName = festivalName;
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

            if (Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                if (!string.IsNullOrEmpty(TargetFestivalName))
                {
                    if (Game1.CurrentEvent.FestivalName != TargetFestivalName)
                    {
                        return;
                    }
                }

                Complete = true;
                Game1.playSound("questcomplete");
            }
        }
    }
}