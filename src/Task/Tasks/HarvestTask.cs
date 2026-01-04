using DeluxeJournal.Automation; 
using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;

using static DeluxeJournal.Task.ITask;
using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class HarvestTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter(TaskParameterNames.Location, TaskParameterTag.FarmLocation, Required = false, Constraints = Constraint.None)]
            public string LocationName { get; set; } = string.Empty;

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.FarmLocation;

            protected override void InitializeInternal(ITask task)
            {
                if (task is HarvestTask harvestTask && !string.IsNullOrEmpty(harvestTask.LocationName))
                {
                    LocationName = harvestTask.LocationName;
                }
            }

            protected override ITask? CreateInternal(string name)
            {
                if (string.IsNullOrWhiteSpace(LocationName))
                {
                    int totalCount = 0;
                    Utility.ForEachLocation(loc =>
                    {
                        totalCount += loc.GetTotalCropsReadyForHarvestExcludingForagables();
                        return true;
                    });
                    return new HarvestTask(name, string.Empty, Math.Max(1, totalCount));
                }

                GameLocation? location = Game1.getLocationFromName(LocationName);

                if (location == null)
                {
                    foreach (var loc in Game1.locations)
                    {
                        if (loc.DisplayName.Equals(LocationName, StringComparison.OrdinalIgnoreCase) || 
                            loc.Name.Equals(LocationName, StringComparison.OrdinalIgnoreCase))
                        {
                            location = loc;
                            break;
                        }
                    }
                }

                if (location != null)
                {
                    int count = location.GetTotalCropsReadyForHarvestExcludingForagables();
                    return new HarvestTask(name, location.Name, Math.Max(1, count));
                }
                return null;
            }
        }

        public string LocationName { get; set; } = string.Empty;

        public HarvestTask() : base(TaskTypes.Harvest) 
        {
            RenewPeriod = Period.Daily;
        }

        public HarvestTask(string name, string locationName, int count) : base(TaskTypes.Harvest, name)
        {
            LocationName = locationName;
            MaxCount = count;
            RenewPeriod = Period.Daily;
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

            int remaining = 0;

            Utility.ForEachLocation(loc =>
            {
                if (!string.IsNullOrEmpty(LocationName) && loc.Name != LocationName) 
                    return true;

                remaining += loc.GetTotalCropsReadyForHarvestExcludingForagables();
                return true;
            });

            int progress = Math.Max(0, MaxCount - remaining);
            
            if (progress != Count) Count = progress;

            if (Active && remaining == 0)
            {
                Count = MaxCount;
                Complete = true; 
                Game1.playSound("questcomplete");
            }
            else if (Complete && remaining > 0)
            {
                Complete = false;
            }
        }
    }
}