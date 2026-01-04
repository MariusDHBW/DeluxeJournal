using DeluxeJournal.Events;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task.Tasks
{
    internal class ShopVisitTask : TaskBase
    {
        public class Factory : TaskFactory
        {
            [TaskParameter("shop", TaskParameterTag.Shop)]
            public string? ShopId { get; set; }
            public override SmartIconFlags EnabledSmartIcons => SmartIconFlags.Shop;
            protected override void InitializeInternal(ITask task)
            {
                if (task is ShopVisitTask shopTask)
                {
                    ShopId = shopTask.TargetShopId;
                }
            }

            protected override ITask? CreateInternal(string name)
            {
                if (string.IsNullOrWhiteSpace(ShopId)) return null;

                var allShops = Game1.content.Load<Dictionary<string, StardewValley.GameData.Shops.ShopData>>("Data/Shops");
                if (!allShops.ContainsKey(ShopId))
                {
                    return null;
                }

                return new ShopVisitTask(name, ShopId);
            }
        }

        public string TargetShopId { get; set; } = string.Empty;

        public ShopVisitTask() : base(TaskTypes.ShopVisit) { }

        public ShopVisitTask(string name, string targetShopId) : base(TaskTypes.ShopVisit, name) 
        {
            TargetShopId = targetShopId;
        }

        public override void EventSubscribe(ITaskEvents events)
        {
            if (DeluxeJournalMod.Instance?.Helper != null)
                DeluxeJournalMod.Instance.Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        public override void EventUnsubscribe(ITaskEvents events)
        {
            if (DeluxeJournalMod.Instance?.Helper != null)
                DeluxeJournalMod.Instance.Helper.Events.Display.MenuChanged -= OnMenuChanged;
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (!CanUpdate()) return;
            if (DeluxeJournalMod.Instance == null) return;

            if (e.NewMenu is ShopMenu shopMenu)
            {
                if (shopMenu.ShopId == TargetShopId)
                {
                    Complete = true;
                    Game1.playSound("questcomplete");
                }
            }
        }
    }
}