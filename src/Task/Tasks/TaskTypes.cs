using Microsoft.Xna.Framework;

namespace DeluxeJournal.Task.Tasks
{
    internal static class TaskTypes
    {
        // --- STANDARD DJ TYPES ---
        public static readonly string Basic = Register("basic", typeof(BasicTask), typeof(BasicFactory<BasicTask>), 0, 0);
        public static readonly string Header = Register("header", typeof(HeaderTask), typeof(BasicFactory<HeaderTask>), 0, 0);
        public static readonly string Collect = Register("collect", typeof(CollectTask), typeof(CollectTask.Factory), 1, 32);
        public static readonly string Craft = Register("craft", typeof(CraftTask), typeof(CraftTask.Factory), 2, 30);
        public static readonly string Blacksmith = Register("blacksmith", typeof(BlacksmithTask), typeof(BlacksmithTask.Factory), 3, 34);
        public static readonly string Build = Register("build", typeof(BuildTask), typeof(BuildTask.Factory), 4, 33);
        public static readonly string Animal = Register("animal", typeof(AnimalTask), typeof(AnimalTask.Factory), 5, 33);
        public static readonly string Gift = Register("gift", typeof(GiftTask), typeof(GiftTask.Factory), 6, 10);
        public static readonly string Buy = Register("buy", typeof(BuyTask), typeof(BuyTask.Factory), 7, 16);
        public static readonly string Sell = Register("sell", typeof(SellTask), typeof(SellTask.Factory), 8, 15);

        // --- UNSERE SMART TASKS---
        public static readonly string Harvest = Register("harvest", typeof(HarvestTask), typeof(HarvestTask.Factory), 1, 20);
        public static readonly string Water = Register("water", typeof(WaterTask), typeof(WaterTask.Factory), 4, 21);
        public static readonly string Forage = Register("forage", typeof(ForageTask), typeof(ForageTask.Factory), 5, 33);
        public static readonly string Machine = Register("machine", typeof(MachineTask), typeof(MachineTask.Factory), 2, 22);
        public static readonly string FarmAnimal = Register("farm_animal", typeof(FarmAnimalTask), typeof(FarmAnimalTask.Factory), 5, 30);
        public static readonly string Pet = Register("pet", typeof(PetTask), typeof(PetTask.Factory), 6, 31);
        public static readonly string PetBowl = Register("pet_bowl", typeof(PetBowlTask), typeof(BasicFactory<PetBowlTask>), 4, 32);
        public static readonly string BulletinBoard = Register("bulletin_board", typeof(BulletinBoardTask), typeof(BasicFactory<BulletinBoardTask>), 0, 26);
        public static readonly string SpecialOrder = Register("special_order", typeof(SpecialOrderTask), typeof(SpecialOrderTask.Factory), 0, 27);
        public static readonly string ActiveFestival = Register("active_festival", typeof(ActiveFestivalTask), typeof(ActiveFestivalTask.Factory), 0, 28);
        public static readonly string PassiveFestival = Register("passive_festival", typeof(PassiveFestivalTask), typeof(PassiveFestivalTask.Factory), 0, 29);
        public static readonly string ShopVisit = Register("shop_visit", typeof(ShopVisitTask), typeof(ShopVisitTask.Factory), 7, 25);
        public static readonly string QueenOfSauce = Register("queen_of_sauce", typeof(QueenOfSauceTask), typeof(BasicFactory<QueenOfSauceTask>), 0, 24);

        private static string Register(string id, Type taskType, Type factoryType, int iconTileSheetIndex, int priority)
        {
            TaskRegistry.Register(id, taskType, factoryType, new TaskIcon(null, new Rectangle(iconTileSheetIndex * 14, 96, 14, 14)), priority);
            return id;
        }
    }
}
