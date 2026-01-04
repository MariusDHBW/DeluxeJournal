using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using StardewValley.SpecialOrders; 
using StardewValley.GameData.SpecialOrders;
using DeluxeJournal.Task;      
using DeluxeJournal.Task.Tasks; 
using StardewValley.Characters;
using StardewValley.Buildings;
using StardewValley.TokenizableStrings;

namespace DeluxeJournal.Automation
{
    public class ToDoListGenerator(IModHelper helper)
    {
        private readonly IModHelper Helper = helper;
        private static readonly StardewValley.Objects.TV _tv = new();

        public static bool IsAutomatedTask(ITask task) 
        {
            return task != null && task.TypeID.StartsWith("auto_");
        }

        public List<ITask> GetDailyTasks()
        {
            var newTasks = new List<ITask>();

            if (DeluxeJournalMod.Config == null || !DeluxeJournalMod.Config.EnableAutomation) 
            return newTasks;

            var cfg = DeluxeJournalMod.Config;

            if (cfg.EnableFestivals) 
            {
                CheckActiveFestivals(newTasks);
                CheckPassiveFestivals(newTasks);
            }
            
            if (cfg.EnableBookseller) CheckBookseller(newTasks);

            if (cfg.EnableTravelingMerchant)
            {
                if (Game1.dayOfMonth % 7 == 5 || Game1.dayOfMonth % 7 == 0)
                {
                AddAutoTask(newTasks, new ShopVisitTask(Helper.Translation.Get("task.auto.traveling_merchant"), Game1.shop_travelingCart), "shop_travelingCart");
                }
            }

            if (cfg.EnableQuests)
            {
                CheckBulletinBoard(newTasks);
                CheckSpecialOrders(newTasks);
            }

            if (cfg.EnableTruffleTask) CheckForage(newTasks, "task.truffle", ["Farm"], "430");

            if (cfg.EnableBirthdays) CheckBirthdays(newTasks);
            if (cfg.EnableGifting) CheckGifting(newTasks);
            
            if (cfg.EnablePet) CheckPet(newTasks);
            if (cfg.EnablePetBowl) CheckPetBowl(newTasks);

            if (cfg.EnableFarmAnimals) CheckFarmAnimals(newTasks);

            if (cfg.EnableFarmStatus || cfg.EnableMachines) CheckFarmStatus(newTasks);
            if (cfg.EnableFarmCave)     CheckForage(newTasks, "task.farm_cave", ["FarmCave"]);

            if (cfg.EnableTools) CheckTools(newTasks);
            if (cfg.EnableQueenOfSauce) CheckQueenOfSauce(newTasks);
            
            if (cfg.EnableLuck) CheckLuck(newTasks);

            return newTasks;
        }

        private void CheckActiveFestivals(List<ITask> tasks)
        {
            string festivalKey = $"{Utility.getSeasonKey(Game1.season)}{Game1.dayOfMonth}";
            if (DataLoader.Festivals_FestivalDates(Game1.temporaryContent).TryGetValue(festivalKey, out string? festivalName))
            {
                if (Event.tryToLoadFestivalData(festivalKey, out _, out _, out _, out int startTime, out _))
                {
                    string text = Helper.Translation.Get("task.auto.festival.active", new { name = festivalName, time = startTime / 100 });
                    AddAutoTask(tasks, new ActiveFestivalTask(text, festivalName), $"festival_{festivalName}");               
                }
            }
        }

        private void CheckPassiveFestivals(List<ITask> tasks)
        {
            if (Utility.TryGetPassiveFestivalDataForDay(Game1.dayOfMonth, Game1.season, null, out string id, out PassiveFestivalData data))
            {
                string realName = TokenParser.ParseText(data.DisplayName) ?? id;
                string text = Helper.Translation.Get("task.auto.festival.passive", new { name = realName });
                AddAutoTask(tasks, new PassiveFestivalTask(text, id), $"passive_festival_{id}");            
            }
        }
        
        private void CheckBulletinBoard(List<ITask> tasks)
        {
            if (Game1.CanAcceptDailyQuest())
            {
                AddAutoTask(tasks, new BulletinBoardTask(Helper.Translation.Get("task.auto.bulletin_board")), "bulletin_board");
            }
        }

        private void CheckSpecialOrders(List<ITask> tasks)
        {
            if (SpecialOrder.IsSpecialOrdersBoardUnlocked())
                CheckSpecificBoard(tasks, "", Helper.Translation.Get("task.auto.special_order.city"));

            if (Game1.netWorldState.Value.GoldenWalnutsFound >= 100)
                CheckSpecificBoard(tasks, "Qi", Helper.Translation.Get("task.auto.special_order.qi"));

            var hiddenTypes = (DeluxeJournalMod.Config?.HiddenSpecialOrderTypes ?? "")
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet();

            Dictionary<string, SpecialOrderData> allOrders = DataLoader.SpecialOrders(Game1.temporaryContent);
            HashSet<string> moddedTypes = [];

            foreach (var order in allOrders.Values)
            {
                if (order.OrderType != "" && order.OrderType != "Qi" && !hiddenTypes.Contains(order.OrderType)) 
                {
                    moddedTypes.Add(order.OrderType);
                }
            }

            foreach (string type in moddedTypes)
            {
                CheckSpecificBoard(tasks, type, Helper.Translation.Get("task.auto.special_order.modded", new { type }));
            }
        }

        private static void CheckSpecificBoard(List<ITask> tasks, string boardType, string taskText)
        {
            if (Game1.player.team.acceptedSpecialOrderTypes.Contains(boardType)) return;
            bool available = Game1.player.team.GetAvailableSpecialOrder(0, boardType) != null || 
                             Game1.player.team.GetAvailableSpecialOrder(1, boardType) != null;
            if (available) 
            {
                AddAutoTask(tasks, new SpecialOrderTask(taskText, boardType), $"special_order_{boardType}");
            }
        }

        private void CheckTools(List<ITask> tasks)
        {
            Farmer player = Game1.player;

            if (player.toolBeingUpgraded.Value is Item toolItem && player.daysLeftForToolUpgrade.Value <= 0)
            {
                bool isClintOpen = true;
                if (Utility.isFestivalDay(Game1.dayOfMonth, Game1.season)) isClintOpen = false;
                else if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) == "Fri" && !player.hasCompletedCommunityCenter() && !Game1.isRaining) isClintOpen = false;
                
                if (isClintOpen)
                {
                    string text = Helper.Translation.Get("task.auto.blacksmith", new { tool = toolItem.DisplayName });
                    AddAutoTask(tasks, new BlacksmithTask(text, toolItem.QualifiedItemId), $"tool_{toolItem.Name}");
                }
            }
        }

        private void CheckQueenOfSauce(List<ITask> tasks)
        {
            uint daysPlayed = Game1.stats.DaysPlayed;
            if (daysPlayed < 5) return;
            int weekNum = (int)(daysPlayed % 224U / 7U);
            if (daysPlayed % 224U == 0U) weekNum = 32;
            DayOfWeek dayOfWeek = (DayOfWeek)(Game1.dayOfMonth % 7);
            
            if (dayOfWeek == DayOfWeek.Wednesday)
            {
                if (Game1.player.team.lastDayQueenOfSauceRerunUpdated.Value != Game1.Date.TotalDays)
                {
                    Game1.player.team.lastDayQueenOfSauceRerunUpdated.Set(Game1.Date.TotalDays);
                    try {
                        int rerunWeek = Helper.Reflection.GetMethod(_tv, "getRerunWeek").Invoke<int>();
                        Game1.player.team.queenOfSauceRerunWeek.Set(rerunWeek);
                    } catch { }
                }
                weekNum = Game1.player.team.queenOfSauceRerunWeek.Value;
            }
            else if (dayOfWeek != DayOfWeek.Sunday) return;

            Dictionary<string, string> cookingChannelRecipes = DataLoader.Tv_CookingChannel(Game1.temporaryContent);
            if (cookingChannelRecipes.TryGetValue($"{weekNum}", out string? translation) && translation != null)
            {
                string recipeName = translation.Split('/')[0];
                if (!Game1.player.cookingRecipes.ContainsKey(recipeName))
                {
                    AddAutoTask(tasks, new QueenOfSauceTask(Helper.Translation.Get("task.queen_of_sauce") + ": " + recipeName, recipeName), $"qos_{recipeName}");
                }
            }
        }

        private void CheckFarmStatus(List<ITask> tasks)
        {
            var cfg = DeluxeJournalMod.Config;

            if (cfg == null) return;

            Utility.ForEachLocation((location) =>
            {
                if (location == null) return true;
                string locId = location.Name; 
                string locDisplay = location.DisplayName ?? location.Name;

                if (cfg.EnableFarmStatus)
                {
                    int readyCrops = location.GetTotalCropsReadyForHarvestExcludingForagables();
                    int cropsToWater = location.GetTotalUnwateredCropsExcludingGinger();

                    if (readyCrops > 0) 
                        AddAutoTask(tasks, new HarvestTask(Helper.Translation.Get("task.auto.harvest", new { count = readyCrops, location = locDisplay }), locId, readyCrops), $"harvest_{locId}");
                
                    if (cropsToWater > 0) 
                        AddAutoTask(tasks, new WaterTask(Helper.Translation.Get("task.auto.water", new { count = cropsToWater, location = locDisplay }), locId, cropsToWater), $"water_{locId}");
                }
                
                if (cfg.EnableMachines)
                {
                    int readyMachines = location.GetNumberOfReadyMachinesExcludingBuildings();
                    if (readyMachines > 0) 
                        AddAutoTask(tasks, new MachineTask(Helper.Translation.Get("task.auto.machine", new { location = locDisplay }), locId, string.Empty), $"machine_{locId}");
                }

                return true;
            });
        }

        private void CheckBookseller(List<ITask> tasks)
        {
            if (Utility.getDaysOfBooksellerThisSeason().Contains(Game1.dayOfMonth))
            {
                AddAutoTask(tasks, new ShopVisitTask(Helper.Translation.Get("task.auto.bookseller"), "Bookseller"), "shop_bookseller");
            }
        }

        private void CheckPet(List<ITask> tasks)
        {
            bool ignoreMaxed = DeluxeJournalMod.Config?.IgnoreMaxedPets ?? false;

            Utility.ForEachCharacter((npc) =>
            {
                if (npc is Pet pet)
                {
                    if (ignoreMaxed && pet.friendshipTowardFarmer.Value >= 1000)
                    {
                        return true;
                    }

                    bool pettedToday = false;  
                    if (pet.lastPetDay.TryGetValue(Game1.player.UniqueMultiplayerID, out int dayPetted))
                        if (dayPetted == Game1.Date.TotalDays) pettedToday = true;

                    if (!pettedToday)
                    {
                        AddAutoTask(tasks, new PetTask(Helper.Translation.Get("task.auto.pet", new { name = pet.displayName }), pet.displayName), $"pet_{pet.Name}");
                    }
                }
                return true;
            });
        }

        private void CheckGifting(List<ITask> tasks)
        {
            bool ignoreMaxed = DeluxeJournalMod.Config?.IgnoreMaxedVillagers ?? false;

            Utility.ForEachCharacter((npc) =>
            {
                if (npc.CanReceiveGifts() && Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship))
                {
                    if (ignoreMaxed && friendship.Points >= 2500)
                    {
                        return true;
                    }

                    if (friendship.GiftsThisWeek < 2 && !npc.isBirthday())
                    {
                        AddAutoTask(tasks, new GiftTask(Helper.Translation.Get("task.auto.gifting", new { name = npc.displayName, count = friendship.GiftsThisWeek }), npc.Name, [], 0), $"gift_{npc.Name}");
                    }
                }
                return true;
            });
        }

        private void CheckFarmAnimals(List<ITask> tasks)
        {
            int unpetCount = 0;
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
                        
                        if (ignoreMaxed && animal.friendshipTowardFarmer.Value >= 1000) continue;

                        if (animal.wasPet.Value) continue;

                        bool hasAutoPetterInHome = animal.home?.indoors.Value?.HasAutoPetter() ?? false;

                        if (!hasAutoPetterInHome) unpetCount++;
                    }
                }
                return true;
            });

            if (unpetCount > 0) 
            {
                AddAutoTask(tasks, new FarmAnimalTask(Helper.Translation.Get("task.auto.farm_animals"), null), "farm_animals");
            }
        }

        private void CheckLuck(List<ITask> tasks)
        {
            double luck = Game1.player.team.sharedDailyLuck.Value;
            string key = luck > 0.07 ? "very_happy" : luck > 0.02 ? "good" : luck > -0.02 ? "neutral" : luck >= -0.07 ? "annoyed" : "very_angry";
            AddAutoTask(tasks, new BasicTask(Helper.Translation.Get("task.auto.luck." + key)), "luck");
        }


        private void CheckBirthdays(List<ITask> tasks)
        {
            bool ignoreMaxed = DeluxeJournalMod.Config?.IgnoreMaxedVillagers ?? false;

            Utility.ForEachCharacter((npc) => 
            {
                if (!npc.CanReceiveGifts() || !npc.isBirthday() || !Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship)) return true;
                if (ignoreMaxed && friendship.Points >= 2500) 
                {
                    return true;
                }
                    AddAutoTask(tasks, new GiftTask(Helper.Translation.Get("task.auto.birthday", new { name = npc.displayName }), npc.Name, [], 0), $"birthday_{npc.Name}");
                return true;
            });
        }

        private void CheckPetBowl(List<ITask> tasks)
        {
            var bowls = Game1.getFarm().buildings.OfType<PetBowl>();
            bool needWater = false;
            
            foreach (var bowl in bowls)
            {
                if (!bowl.watered.Value)
                {
                    needWater = true;
                    break;
                }
            }

            if (needWater)
            {
                AddAutoTask(tasks, new PetBowlTask(Helper.Translation.Get("task.auto.pet_bowl")), "pet_bowl");
            }
        }

        private void CheckForage(List<ITask> tasks, string taskKey, List<string> locations, string? itemIdFilter = null)
        {
            int count = 0;
            string locationDisplayName = "";

            foreach (string locName in locations)
            {
                GameLocation? location = Game1.getLocationFromName(locName);
                if (location == null) continue;

                if (string.IsNullOrEmpty(locationDisplayName))
                {
                    locationDisplayName = location.DisplayName ?? location.Name;
                }

                foreach (var obj in location.objects.Values)
                {
                    bool match = false;
                    if (itemIdFilter != null)
                        match = obj.ItemId == itemIdFilter || obj.QualifiedItemId == itemIdFilter || obj.QualifiedItemId == "(O)" + itemIdFilter;
                    else
                        match = obj.IsSpawnedObject || (obj.QualifiedItemId == "(BC)128" && obj.readyForHarvest.Value);

                    if (match) count++;
            }
            }

            if (count > 0) 
            {
                string name = Helper.Translation.Get(taskKey).Default("Gather Items").ToString();
                
                if (!string.IsNullOrEmpty(locationDisplayName))
                {
                    name += $" ({locationDisplayName})";
                }

                AddAutoTask(tasks, new ForageTask(name, count, locations, itemIdFilter), $"forage_{taskKey}");
            }
        }

        private static void AddAutoTask(List<ITask> tasks, ITask newTask, string uniqueSuffix)
        {
            newTask.TypeID = "auto_" + uniqueSuffix;
            tasks.Add(newTask);
        }
    }
}