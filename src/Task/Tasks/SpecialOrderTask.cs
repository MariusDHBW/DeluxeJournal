using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;
using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class SpecialOrderTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter(TaskParameterNames.SpecialOrderType, TaskParameterTag.SpecialOrderType)]
            public string BoardType { get; set; } = string.Empty;

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.SpecialOrder;

            protected override void InitializeInternal(ITask task)
            {
                if (task is SpecialOrderTask orderTask) BoardType = orderTask.BoardType;
            }

            protected override ITask? CreateInternal(string name)
            {
                return new SpecialOrderTask(name, BoardType);
            }
        }

        public string BoardType { get; set; } = "";

        public SpecialOrderTask() : base(TaskTypes.SpecialOrder) { }

        public SpecialOrderTask(string name, string boardType) : base(TaskTypes.SpecialOrder, name) 
        {
            BoardType = boardType;
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
            if (Game1.player.team.acceptedSpecialOrderTypes.Contains(BoardType))
            {
                Complete = true;
                return;
            }
            bool left = Game1.player.team.GetAvailableSpecialOrder(0, BoardType) != null;
            bool right = Game1.player.team.GetAvailableSpecialOrder(1, BoardType) != null;
            if (!left && !right)
            {
                Complete = true; 
                Game1.playSound("questcomplete");
            }
        }
    }
}