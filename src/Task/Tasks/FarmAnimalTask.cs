using DeluxeJournal.Events;
using DeluxeJournal.Automation;
using StardewModdingAPI.Events;
using StardewValley;


using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class FarmAnimalTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter(TaskParameterNames.Building, TaskParameterTag.AnimalLocation, Required = false, Constraints = Constraint.None)]
            public string LocationName { get; set; } = string.Empty;
            
            [TaskParameter(TaskParameterNames.FarmAnimal, TaskParameterTag.FarmAnimalList, Required = false, Constraints = Constraint.None)]
            public IList<string>? AnimalIds { get; set; }

            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.Animal | SmartIconFlags.AnimalLocation;

            public override bool EnableSmartIconCount => true;

            protected override void InitializeInternal(ITask task)
            {
                if (task is FarmAnimalTask animalTask)
                {
                    AnimalIds = animalTask.TargetAnimalIds;
                    LocationName = animalTask.LocationName;
                }
            }

            protected override ITask? CreateInternal(string name)
            {
                return new FarmAnimalTask(name, AnimalIds, LocationName);
            }
        }

        public IList<string> TargetAnimalIds { get; set; }
        
        public string LocationName { get; set; }

        public FarmAnimalTask() : base(TaskTypes.FarmAnimal) 
        {
            TargetAnimalIds = new List<string>();
            LocationName = string.Empty;
        }

        public FarmAnimalTask(string name, IList<string>? targetAnimalIds, string locationName = "") : base(TaskTypes.FarmAnimal, name) 
        {
            TargetAnimalIds = targetAnimalIds ?? new List<string>();
            LocationName = locationName;
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

            int pettedOrAutoCount = 0;
            int totalCount = 0;
            long myID = Game1.player.UniqueMultiplayerID;
            bool ignoreMaxed = DeluxeJournalMod.Config?.IgnoreMaxedAnimals ?? false;

            Utility.ForEachLocation((location) =>
            {
                if (location != null && location.animals.Length > 0)
                {
                    bool hasAutoPetter = location.HasAutoPetter();

                    foreach (var animal in location.animals.Values)
                    {
                        if (animal.ownerID.Value != myID) continue;

                        if (!string.IsNullOrEmpty(LocationName))
                        {
                            bool match = false;

                            if (animal.home != null && animal.home.id.Value.ToString() == LocationName)
                            {
                                match = true;
                            }
                            else if (animal.home?.indoors?.Value?.Name == LocationName)
                            {
                                match = true;
                            }

                            if (!match) continue;
                        }
                        
                        if (TargetAnimalIds.Count > 0 && !TargetAnimalIds.Contains(animal.type.Value)) continue;
                        
                        if (ignoreMaxed && animal.friendshipTowardFarmer.Value >= 1000) continue;

                        totalCount++;

                        if (animal.wasPet.Value || hasAutoPetter)
                        {
                            pettedOrAutoCount++;
                        }
                    }
                }
                return true;
            });

            if (totalCount != MaxCount) MaxCount = totalCount;
            if (Count != pettedOrAutoCount) Count = pettedOrAutoCount;

            if (Active && pettedOrAutoCount >= totalCount && totalCount > 0)
            {
                Complete = true;
                Game1.playSound("questcomplete");
            }
            else if (Complete && pettedOrAutoCount < totalCount)
            {
                Complete = false; 
            }
        }
    }
}