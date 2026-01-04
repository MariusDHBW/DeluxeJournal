using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;
using static DeluxeJournal.Task.ITask; 
using static DeluxeJournal.Task.TaskParameterAttribute; 

namespace DeluxeJournal.Task.Tasks
{
    public class ForageTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter("location", TaskParameterTag.ForageLocation, InputType = TaskParameterInputType.TextBox)]
            public string LocationName { get; set; } = "";

            [TaskParameter(TaskParameterNames.Item, TaskParameterTag.ForageItemList, Constraints = Constraint.SObject | Constraint.ItemCategory)]
            public IList<string>? ItemIds { get; set; }

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.ForageLocation | SmartIconFlags.ForageItem;

            protected override void InitializeInternal(ITask task)
            {
                if (task is ForageTask forageTask)
                {
                    if (forageTask.LocationNames.Count > 0)
                    {
                        LocationName = forageTask.LocationNames[0];
                    }
                    if (!string.IsNullOrEmpty(forageTask.TargetItemId))
                    {
                        ItemIds = new List<string> { forageTask.TargetItemId };
                    }
                }
            }

            protected override ITask? CreateInternal(string name)
            {
                if (string.IsNullOrWhiteSpace(LocationName)) return null;

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
                    string? targetId = (ItemIds != null && ItemIds.Count > 0) ? ItemIds[0] : null;

                    int count = 0;
                    foreach (var obj in location.objects.Values)
                    {
                        if (IsMatch(obj, targetId))
                        {
                            count++;
                        }
                    }

                    return new ForageTask(name, Math.Max(1, count), new List<string> { location.Name }, targetId);
                }

                return null;
            }
        }

        public List<string> LocationNames { get; set; } = [];
        public string? TargetItemId { get; set; }
        public ForageTask() : base(TaskTypes.Forage) 
        {
            RenewPeriod = Period.Daily;
        }

        public ForageTask(string name, int count, List<string> locationNames, string? targetItemId = null) 
            : base(TaskTypes.Forage, name)
        {
            MaxCount = count;
            Count = 0;
            RenewPeriod = Period.Daily;
            LocationNames = locationNames;
            TargetItemId = targetItemId;
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

            int currentCount = 0;

            foreach (string locName in LocationNames)
            {
                GameLocation? location = Game1.getLocationFromName(locName);
                if (location == null) continue;

                foreach (var obj in location.objects.Values)
                {
                    if (IsMatch(obj, TargetItemId))
                    {
                        currentCount++;
                    }
                }
            }

            int progress = Math.Max(0, MaxCount - currentCount);

            if (progress != Count) Count = progress;

            if (Active && currentCount == 0)
            {
                Count = MaxCount;
                Complete = true;
                Game1.playSound("questcomplete");
            }
            else if (Complete && currentCount > 0)
            {
                Complete = false;
            }
        }

        /// <summary>
        /// Die zentrale Filter-Logik.
        /// </summary>
        private static bool IsMatch(SObject obj, string? filterId)
        {
            if (!string.IsNullOrEmpty(filterId))
            {
                if (filterId.StartsWith("-") && int.TryParse(filterId, out int category))
                {
                    return obj.Category == category && obj.IsSpawnedObject;
                }
                
                bool isItemMatch = obj.ItemId == filterId || obj.QualifiedItemId == filterId || obj.QualifiedItemId == "(O)" + filterId;
                
                return isItemMatch;
            }
            else
            {
                return obj.IsSpawnedObject || (obj.QualifiedItemId == "(BC)128" && obj.readyForHarvest.Value);
            }
        }

        public override ITask Copy()
        {
            return new ForageTask(Name, MaxCount, [.. LocationNames], TargetItemId)
            {
                Count = Count,
                Complete = Complete,
                RenewPeriod = RenewPeriod,
                RenewDate = new WorldDate(RenewDate),
                RequiredSeasons = RequiredSeasons,
                RequiredWeather = RequiredWeather,
                RequiredWeeks = RequiredWeeks,
                RequiredWeekdays = RequiredWeekdays
            };
        }
    }
}