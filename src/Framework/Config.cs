using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace DeluxeJournal.Framework
{
    internal class Config
    {
        // --- AUTOMATION SETTINGS ---
        public bool EnableAutomation { get; set; } = true;
        public bool EnableFestivals { get; set; } = true;
        public bool EnableBirthdays { get; set; } = true;
        public bool EnableGifting { get; set; } = true;
        public bool EnableFarmStatus { get; set; } = true;
        public bool EnableMachines { get; set; } = true;
        public bool EnableFarmCave { get; set; } = true; 
        public bool EnableTruffleTask { get; set; } = true;
        public bool EnableFarmAnimals { get; set; } = true;
        public bool IgnoreMaxedAnimals { get; set; } = false;
        public bool IgnoreMaxedPets { get; set; } = false;
        public bool IgnoreMaxedVillagers { get; set; } = false;
        public bool EnableTools { get; set; } = true;
        public bool EnableQueenOfSauce { get; set; } = true;
        public bool EnableTravelingMerchant { get; set; } = true;
        public bool EnableBookseller { get; set; } = true;
        public bool EnablePet { get; set; } = true;
        public bool EnablePetBowl { get; set; } = true;
        public bool EnableLuck { get; set; } = true;
        public bool EnableQuests { get; set; } = true;
        public string HiddenSpecialOrderTypes { get; set; } = "";
        
        // --- JOURNAL INTERFACE (Unverändert) ---
        /// <summary>Enable to push renewed tasks to the top of the task group instead of the bottom.</summary>
        public bool PushRenewedTasksToTheTop { get; set; } = false;

        /// <summary>Enable to have the "Smart Add" button be the default when creating a task (if applicable).</summary>
        public bool EnableDefaultSmartAdd { get; set; } = true;

        /// <summary>Enable to show an indicator on the journal button when a task is completed.</summary>
        public bool EnableVisualTaskCompleteIndicator { get; set; } = false;

        /// <summary>Show the "Smart Add" info box in the "Add Task" window.</summary>
        public bool ShowSmartAddTip { get; set; } = true;

        /// <summary>Show the help message when the task page is empty.</summary>
        public bool ShowAddTaskHelpMessage { get; set; } = true;

        /// <summary>Toggle between "Net Wealth" and "Total Amount to Pay/Gain" display modes.</summary>
        public bool MoneyViewNetWealth { get; set; } = false;

        /// <summary>Keybind for toggling the visibility of overlays.</summary>
        public KeybindList ToggleOverlaysKeybind { get; set; } = KeybindList.Parse("O");

        /// <summary>Overlay background color hex code (alpha normalized RGB values for blending).</summary>
        public string OverlayBackgroundColor { get; set; } = "00000040";

        /// <summary>The name of the color schema file to load from "assets/data/colors/". Uses the default loading rules if empty.</summary>
        public string TargetColorSchemaFile { get; set; } = string.Empty;

        /// <summary>Save data to the mod configuration file.</summary>
        public void Save()
        {
            if (DeluxeJournalMod.Instance is not DeluxeJournalMod mod)
            {
                throw new InvalidOperationException("Attempted to save config before mod entry.");
            }

            mod.Helper.WriteConfig(this);
        }

        public void RegisterMenu(IManifest manifest, IGenericModConfigMenuApi configMenu)
        {
            Config GetConfig() => DeluxeJournalMod.Config ?? this;
            var helper = DeluxeJournalMod.Instance?.Helper;

            if (helper == null) return;

            configMenu.Register(
                manifest,
                reset: () => { DeluxeJournalMod.Config = new Config(); },
                save: () => { GetConfig().Save(); DeluxeJournalMod.Instance?.ReloadDailyTasks(); }
            );

            // --- AUTOMATION SECTION ---
            configMenu.AddSectionTitle(manifest, () => helper.Translation.Get("config.automation.section"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableAutomation, val => GetConfig().EnableAutomation = val,
                () => helper.Translation.Get("config.automation.enable"), () => helper.Translation.Get("config.automation.enable.desc"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableFestivals, val => GetConfig().EnableFestivals = val,
                () => helper.Translation.Get("config.automation.festivals"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableBirthdays, val => GetConfig().EnableBirthdays = val,
                () => helper.Translation.Get("config.automation.birthdays"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableGifting, val => GetConfig().EnableGifting = val,
                () => helper.Translation.Get("config.automation.gifting"));

            configMenu.AddBoolOption(manifest, () => GetConfig().IgnoreMaxedVillagers, val => GetConfig().IgnoreMaxedVillagers = val,
                () => helper.Translation.Get("config.automation.ignore_maxed_villagers"), () => helper.Translation.Get("config.automation.ignore_maxed_villagers.desc"));

            // ANIMALS
            configMenu.AddBoolOption(manifest, () => GetConfig().EnableFarmAnimals, val => GetConfig().EnableFarmAnimals = val,
                () => helper.Translation.Get("config.automation.farm_animals"));

            configMenu.AddBoolOption(manifest, () => GetConfig().IgnoreMaxedAnimals, val => GetConfig().IgnoreMaxedAnimals = val,
                () => helper.Translation.Get("config.automation.ignore_maxed_animals"), () => helper.Translation.Get("config.automation.ignore_maxed_animals.desc"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnablePet, val => GetConfig().EnablePet = val,
                () => helper.Translation.Get("config.automation.pet"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnablePetBowl, val => GetConfig().EnablePetBowl = val,
                () => helper.Translation.Get("config.automation.pet_bowl"), () => helper.Translation.Get("config.automation.pet_bowl.desc"));

            configMenu.AddBoolOption(manifest, () => GetConfig().IgnoreMaxedPets, val => GetConfig().IgnoreMaxedPets = val,
                () => helper.Translation.Get("config.automation.ignore_maxed_pets"), () => helper.Translation.Get("config.automation.ignore_maxed_pets.desc"));

            // FARM STATUS & MACHINES
            configMenu.AddBoolOption(manifest, () => GetConfig().EnableFarmStatus, val => GetConfig().EnableFarmStatus = val,
                () => helper.Translation.Get("config.automation.farm_status"), () => helper.Translation.Get("config.automation.farm_status.desc"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableMachines, val => GetConfig().EnableMachines = val,
                () => helper.Translation.Get("config.automation.machines"), () => helper.Translation.Get("config.automation.machines.desc"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableFarmCave, val => GetConfig().EnableFarmCave = val,
                () => helper.Translation.Get("config.automation.farm_cave"), () => helper.Translation.Get("config.automation.farm_cave.desc"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableTruffleTask, val => GetConfig().EnableTruffleTask = val,
                () => helper.Translation.Get("config.automation.truffles"), () => helper.Translation.Get("config.automation.truffles.desc"));

            // MISC
            configMenu.AddBoolOption(manifest, () => GetConfig().EnableTools, val => GetConfig().EnableTools = val,
                () => helper.Translation.Get("config.automation.tools"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableQueenOfSauce, val => GetConfig().EnableQueenOfSauce = val,
                () => helper.Translation.Get("config.automation.qos"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableTravelingMerchant, val => GetConfig().EnableTravelingMerchant = val,
                () => helper.Translation.Get("config.automation.traveling_merchant"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableBookseller, val => GetConfig().EnableBookseller = val,
                () => helper.Translation.Get("config.automation.bookseller"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableLuck, val => GetConfig().EnableLuck = val,
                () => helper.Translation.Get("config.automation.luck"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableQuests, val => GetConfig().EnableQuests = val,
                () => helper.Translation.Get("config.automation.quests"));

            configMenu.AddTextOption(manifest, () => GetConfig().HiddenSpecialOrderTypes, val => GetConfig().HiddenSpecialOrderTypes = val,
                () => helper.Translation.Get("config.automation.hidden_orders"), () => helper.Translation.Get("config.automation.hidden_orders.desc"));

            // --- INTERFACE SECTION ---
            configMenu.AddSectionTitle(manifest, () => helper.Translation.Get("config.interface.section"));

            configMenu.AddKeybindList(manifest, () => GetConfig().ToggleOverlaysKeybind, val => GetConfig().ToggleOverlaysKeybind = val,
                () => helper.Translation.Get("config.interface.overlay_key"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableDefaultSmartAdd, val => GetConfig().EnableDefaultSmartAdd = val,
                () => helper.Translation.Get("config.interface.smart_add_default"));

            configMenu.AddBoolOption(manifest, () => GetConfig().ShowSmartAddTip, val => GetConfig().ShowSmartAddTip = val,
                () => helper.Translation.Get("config.interface.smart_add_tip"));

            configMenu.AddBoolOption(manifest, () => GetConfig().ShowAddTaskHelpMessage, val => GetConfig().ShowAddTaskHelpMessage = val,
                () => helper.Translation.Get("config.interface.help_message"));

            configMenu.AddBoolOption(manifest, () => GetConfig().PushRenewedTasksToTheTop, val => GetConfig().PushRenewedTasksToTheTop = val,
                () => helper.Translation.Get("config.interface.push_top"));

            configMenu.AddBoolOption(manifest, () => GetConfig().EnableVisualTaskCompleteIndicator, val => GetConfig().EnableVisualTaskCompleteIndicator = val,
                () => helper.Translation.Get("config.interface.complete_indicator"));

            configMenu.AddBoolOption(manifest, () => GetConfig().MoneyViewNetWealth, val => GetConfig().MoneyViewNetWealth = val,
                () => helper.Translation.Get("config.interface.net_wealth"));
        }
    }
}