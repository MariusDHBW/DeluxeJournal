using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;

using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class MachineTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter(TaskParameterNames.Building, TaskParameterTag.MachineLocation, Constraints = Constraint.None)]
            public string LocationName { get; set; } = string.Empty;

            [TaskParameter(TaskParameterNames.Machine, TaskParameterTag.Machine, Constraints = Constraint.None)]
            public string MachineId { get; set; } = string.Empty;

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.MachineLocation | SmartIconFlags.Machine;
            public override bool EnableSmartIconCount => true;

            public override bool IsReady() => true; 

            protected override void InitializeInternal(ITask task)
            {
                if (task is MachineTask machineTask)
                {
                    LocationName = machineTask.LocationName;
                    MachineId = machineTask.MachineId;
                }
            }

            protected override ITask? CreateInternal(string name)
            {
                return new MachineTask(name, LocationName, MachineId);
            }
        }

        public string LocationName { get; set; }
        public string MachineId { get; set; }

        public MachineTask() : base(TaskTypes.Machine) 
        {
            LocationName = string.Empty;
            MachineId = string.Empty;
        }

        public MachineTask(string name, string locationName, string machineId) : base(TaskTypes.Machine, name)
        {
            LocationName = locationName;
            MachineId = machineId;
            MaxCount = 0;
            Count = 0;
        }

        public override bool ShouldShowProgress() => MaxCount > 0;

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

            int currentReadyCount = 0;
            GameLocation? targetLocation = null;

            if (!string.IsNullOrEmpty(LocationName))
            {
                targetLocation = Game1.getLocationFromName(LocationName);

                targetLocation ??= Game1.getFarm()?.buildings
                    .FirstOrDefault(b => b.id.Value.ToString() == LocationName)?.indoors.Value;

                if (targetLocation == null) return;
            }

            Utility.ForEachLocation((location) =>
            {
                if (targetLocation != null && location != targetLocation) return true;

                foreach (var obj in location.objects.Values)
                {
                    if (!string.IsNullOrEmpty(MachineId))
                    {
                        if (obj.QualifiedItemId != MachineId && obj.ItemId != MachineId) continue;
                    }

                    if (obj.readyForHarvest.Value && obj.bigCraftable.Value && obj.heldObject.Value != null)
                    {
                        currentReadyCount++;
                    }
                }
                return true;
            });

            if (MaxCount == 0 || currentReadyCount > MaxCount) MaxCount = currentReadyCount;
            int collectedCount = Math.Max(0, MaxCount - currentReadyCount);
            if (Count != collectedCount) Count = collectedCount;

            if (Active && currentReadyCount == 0 && MaxCount > 0)
            {
                Count = MaxCount;
                Complete = true;
                Game1.playSound("questcomplete");
            }
            else if (Complete && currentReadyCount > 0)
            {
                Complete = false;
            }
        }
    }
}