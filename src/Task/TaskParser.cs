using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Tools;
using StardewValley.TokenizableStrings;
using DeluxeJournal.Task.Tasks;
using DeluxeJournal.Util;

using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Task
{
    /// <summary>Parses text into tasks.</summary>
    public class TaskParser
    {
        /// <summary>Mode of operation for parsing.</summary>
        public enum ParseMode
        {
            /// <summary>Default mode. Parse text and create a new factory.</summary>
            CreateFactory,

            /// <summary>Keep the current factory and update its parameters with the parsed values.</summary>
            UpdateFactory,

            /// <summary>Only update the parsed values. Ignore the factory entirely.</summary>
            UpdateValues
        }

        private readonly ITranslationHelper _translation;
        private readonly TaskParserSettings _settings;
        private readonly LocalizedGameDataMaps _localizedGameData;
        private readonly IDictionary<string, HashSet<string>> _keywords;

        private string _id;
        private string _npcName;
        private string _buildingType;
        private string _generalLocation;
        private string _farmLocation;
        private string _forageLocation;
        private string _animalLocation;
        private string _machineLocation;
        private string _shopId;
        private string _petName;
        private string _machineId;
        private string _specialOrderType;
        private string _passiveFestivalId;
        private string _activeFestivalId;
        private int? _count;
        private IEnumerable<string>? _itemIds;
        private IEnumerable<string>? _farmAnimals;
        private TaskFactory? _factory;

        private Item? _cachedItem;
        private string? _cachedNpcDisplayName;
        private string? _cachedBuildingDisplayName;
        private string? _cachedFarmAnimalDisplayName;
        private string? _cachedMachineDisplayName;

        /// <summary>The ID of the matched task.</summary>
        public string ID => _id;

        /// <summary>The parsed count value.</summary>
        public int Count => _count ?? 1;

        /// <summary>Parsed item IDs.</summary>
        public IEnumerable<string>? ItemIds
        {
            get => _itemIds;

            private set
            {
                _itemIds = value;
                _cachedItem = null;
            }
        }

        /// <summary>Parsed item instance that represents the group of item IDs (if there is more than one).</summary>
        public Item? ProxyItem
        {
            get
            {
                if (_cachedItem == null && ItemIds != null && ItemIds.Any())
                {
                    string itemId = ItemIds.First();

                    if (itemId.StartsWith('-'))
                    {
                        if (!int.TryParse(itemId, out int category))
                        {
#if DEBUG
                            if (DeluxeJournalMod.Instance is DeluxeJournalMod instance)
                            {
                                instance.Monitor.Log($"{nameof(TaskParser)}.{nameof(ProxyItem)}: invalid item ID for category group, id='{itemId}'", LogLevel.Debug);
                            }
#endif
                            ItemIds = null;
                            return _cachedItem = null;
                        }

                        return _cachedItem = ItemRegistry.Create(category switch
                        {
                            SObject.GreensCategory => "(O)20",
                            SObject.GemCategory => "(O)80",
                            SObject.VegetableCategory => "(O)24",
                            SObject.FishCategory => "(O)145",
                            SObject.EggCategory => "(O)176",
                            SObject.MilkCategory => "(O)184",
                            _ => null
                        });
                    }

                    if (FlavoredItemHelper.CreateFlavoredItem(itemId) is Item flavoredItem)
                    {
                        return _cachedItem = flavoredItem;
                    }

                    if (ItemRegistry.GetData(itemId)?.RawData is ToolData toolData
                        && ToolHelper.IsToolUpgradable(toolData)
                        && ToolHelper.IsToolBaseUpgradeLevel(toolData))
                    {
                        _cachedItem = ToolHelper.GetToolUpgradeForPlayer(toolData, Game1.player);
                    }

                    if (_cachedItem == null)
                    {
                        _cachedItem = ItemRegistry.Create(itemId, allowNull: true);
                    }
                }

                return _cachedItem;
            }
        }

        /// <summary>Localized display name for the parsed item group.</summary>
        public string ProxyItemDisplayName
        {
            get
            {
                if (ItemIds != null && ProxyItem is Item item)
                {
                    if (ItemIds.First().StartsWith('-'))
                    {
                        return item.Category switch
                        {
                            SObject.GreensCategory => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.568"),
                            SObject.GemCategory => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.569"),
                            SObject.VegetableCategory => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.570"),
                            SObject.FishCategory => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.571"),
                            SObject.EggCategory => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.572"),
                            SObject.MilkCategory => Game1.content.LoadString("Strings\\StringsFromCSFiles:CraftingRecipe.cs.573"),
                            _ => "???"
                        };
                    }

                    return item.DisplayName;
                }

                return string.Empty;
            }
        }

        /// <summary>The parsed NPC's internal name.</summary>
        public string NpcName
        {
            get => _npcName;

            private set
            {
                _npcName = value;
                _cachedNpcDisplayName = null;
            }
        }

        /// <summary>Localized NPC display name.</summary>
        public string NpcDisplayName
        {
            get
            {
                if (_cachedNpcDisplayName == null && Game1.characterData.TryGetValue(_npcName, out var data))
                {
                    _cachedNpcDisplayName = TokenParser.ParseText(data.DisplayName);
                }

                return _cachedNpcDisplayName ?? _npcName;
            }
        }
        
        public string PetName
        {
            get => _petName; private set
            {
                _petName = value;
            }
        }

        public string PetDisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_petName))
                {
                    var pet = Game1.getCharacterFromName(_petName);
                    if (pet != null)
                    {
                        return pet.displayName;
                    }
                }
                return _petName;
            }
        }

        public string ShopId
        {
            get => _shopId;
            private set
            {
                _shopId = value;
            }
        }

        public string ShopDisplayName => _shopId;

        public string MachineId
        {
            get => _machineId;
            private set
            {
                _machineId = value;
                _cachedItem = null;
                _cachedMachineDisplayName = null;
            }
        }

        public string MachineDisplayName
        {
            get
            {
                if (_cachedMachineDisplayName == null && !string.IsNullOrEmpty(_machineId))
                {
                    var data = ItemRegistry.GetData(_machineId);
                    if (data != null)
                    {
                        _cachedMachineDisplayName = data.DisplayName;
                    }
                }
                return _cachedMachineDisplayName ?? _machineId;
            }
        }

        /// <summary>The parsed building type.</summary>
        public string BuildingType
        {
            get => _buildingType;

            private set
            {
                _buildingType = value;
                _cachedBuildingDisplayName = null;
            }
        }

        /// <summary>Localized building display name.</summary>
        public string BuildingDisplayName
        {
            get
            {
                if (_cachedBuildingDisplayName == null && Game1.buildingData.TryGetValue(_buildingType, out var data))
                {
                    _cachedBuildingDisplayName = TokenParser.ParseText(data.Name);
                }

                return _cachedBuildingDisplayName ?? _buildingType;
            }
        }

        /// <summary>Gibt den Anzeigenamen des aktuell gefundenen Ortes zurück (egal welcher Typ).</summary>
        public string LocationName => ResolveLocationName(_generalLocation);
        public string FarmLocationName => ResolveLocationName(_farmLocation);
        public string ForageLocationName => ResolveLocationName(_forageLocation);
        public string AnimalLocationName => ResolveLocationName(_animalLocation);
        public string MachineLocationName => ResolveLocationName(_machineLocation);

        public string LocationDisplayName => ResolveLocationName(_generalLocation);
        public string MachineLocationDisplayName => ResolveLocationName(_machineLocation);
        public string FarmLocationDisplayName => ResolveLocationName(_farmLocation);
        public string ForageLocationDisplayName => ResolveLocationName(_forageLocation);
        public string AnimalLocationDisplayName => ResolveLocationName(_animalLocation);

        public string SpecialOrderType
        {
            get => _specialOrderType; private set
            {
                _specialOrderType = value;
            }
        }

        public string PassiveFestivalId
        {
            get => _passiveFestivalId; private set
            {
                _passiveFestivalId = value;
            }
        }

        public string PassiveFestivalDisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_passiveFestivalId) &&
                    DataLoader.PassiveFestivals(Game1.content).TryGetValue(_passiveFestivalId, out var data))
                {
                    string displayName = TokenParser.ParseText(data.DisplayName);
                    
                    if (string.IsNullOrWhiteSpace(displayName) || displayName.StartsWith("@"))
                    {
                        return System.Text.RegularExpressions.Regex.Replace(_passiveFestivalId, "([a-z])([A-Z])", "$1 $2");
                    }
                    return displayName;
                }
                return _passiveFestivalId;
            }
        }

        public string ActiveFestivalId
        {
            get => _activeFestivalId;
            private set { _activeFestivalId = value; }
        }

        public string ActiveFestivalDisplayName => _activeFestivalId;

        /// <summary>The parsed farm animal name.</summary>
        public IEnumerable<string>? FarmAnimals
        {
            get => _farmAnimals;

            private set
            {
                _farmAnimals = value;
                _cachedFarmAnimalDisplayName = null;
            }
        }

        /// <summary>Localized farm animal display name.</summary>
        public string FarmAnimalDisplayName
        {
            get
            {
                string farmAnimalName = string.Empty;

                if (_cachedFarmAnimalDisplayName == null
                    && FarmAnimals != null
                    && FarmAnimals.Any()
                    && Game1.farmAnimalData.TryGetValue(farmAnimalName = FarmAnimals.First(), out var data))
                {
                    _cachedFarmAnimalDisplayName = TokenParser.ParseText(data.ShopDisplayName ?? data.DisplayName);
                }

                return _cachedFarmAnimalDisplayName ?? farmAnimalName;
            }
        }

        /// <summary><see cref="TaskFactory"/> corresponding to the matched task ID.</summary>
        public TaskFactory Factory
        {
            get => _factory ??= GenerateFactory();

            set
            {
                Type factoryType = value.GetType();

                if (factoryType != TaskRegistry.GetFactoryType(_id))
                {
                    foreach (string id in TaskRegistry.Keys)
                    {
                        if (TaskRegistry.GetFactoryType(id) == factoryType)
                        {
                            _id = id;
                            break;
                        }
                    }
                }

                _factory = value;
            }
        }

        public TaskParser(ITranslationHelper translation) : this(translation, new())
        {
        }

        public TaskParser(ITranslationHelper translation, TaskParserSettings settings)
        {
            _translation = translation;
            _settings = settings;
            _localizedGameData = new LocalizedGameDataMaps(translation, settings, DeluxeJournalMod.Instance?.Monitor);
            _keywords = new Dictionary<string, HashSet<string>>();
            _id = TaskTypes.Basic;
            _npcName = string.Empty;
            _petName = string.Empty;
            _buildingType = string.Empty;
            _machineId = string.Empty;
            _generalLocation = string.Empty;
            _farmLocation = string.Empty;
            _forageLocation = string.Empty;
            _animalLocation = string.Empty;
            _machineLocation = string.Empty;
            _shopId = string.Empty;
            _specialOrderType = string.Empty;
            _passiveFestivalId = string.Empty;
            _activeFestivalId = string.Empty;
            _count = null;

            PopulateKeywords(_keywords);
        }

        /// <summary>Clear cached parser state.</summary>
        /// <param name="excludeType">Exclude type information: ID and <see cref="TaskFactory"/>.</param>
        public void Clear(bool excludeType = false)
        {
            if (!excludeType)
            {
                _id = TaskTypes.Basic;
                _factory = null;
            }

            _count = null;
            _itemIds = null;
            _farmAnimals = null;
            _npcName = string.Empty;
            _petName = string.Empty;
            _buildingType = string.Empty;
            _generalLocation = string.Empty;
            _farmLocation = string.Empty;
            _forageLocation = string.Empty;
            _animalLocation = string.Empty;
            _machineLocation = string.Empty;
            _shopId = string.Empty;
            _machineId = string.Empty;
            _specialOrderType = string.Empty;
            _passiveFestivalId = string.Empty;
            _activeFestivalId = string.Empty;

            _cachedItem = null;
            _cachedNpcDisplayName = null;
            _cachedBuildingDisplayName = null;
            _cachedFarmAnimalDisplayName = null;
            _cachedMachineDisplayName = null;
        }

        /// <summary>Matched a non-basic task.</summary>
        public bool MatchFound()
        {
            return _id != TaskTypes.Basic;
        }

        /// <summary>Parse text and update the parser state with the results.</summary>
        /// <param name="text">Raw text to be parsed.</param>
        /// <param name="mode">Parser mode.</param>
        /// <returns>
        /// <c>true</c> if the parsed text produced a factory in a ready state, or the <paramref name="mode"/>
        /// is <see cref="ParseMode.UpdateValues"/>; <c>false</c> if the text did not match any
        /// task (excluding "basic" tasks).
        /// </returns>
        public bool Parse(string text, ParseMode mode = ParseMode.CreateFactory)
        {
            if (mode == ParseMode.CreateFactory)
            {
                Clear();
            }

            if (text.Length == 0)
            {
                return false;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<int> ignored = new HashSet<int>();
            List<string> keywords = new List<string>();
            char[] delimiters = [ ' ', ',', '.', '/', '?', '!' ];
            string[] words;
            string joinSeparator;

            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh ||
                LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja ||
                LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.th)
            {
                words = text.Trim()
                    .Where(c => !delimiters.Contains(c))
                    .Select(c => c.ToString())
                    .ToArray();
                joinSeparator = string.Empty;
            }
            else
            {
                words = text.Trim().ToLower().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                joinSeparator = " ";
            }

            for (int group = words.Length; group > 0; group--)
            {
                for (int i = words.Length; i >= group; i--)
                {
                    string word = string.Join(joinSeparator, words[(i - group)..i]);

                    if (word.Length == 0)
                    {
                        continue;
                    }

                    for (int j = i - group; j < i; j++)
                    {
                        if (ignored.Contains(j))
                        {
                            goto skip;
                        }
                    }

                    if (mode == ParseMode.CreateFactory && _keywords.ContainsKey(word))
                    {
                        keywords.Add(word);
                    }
                    else if (!HandleWord(word))
                    {
                        continue;
                    }

                    for (int j = i - group; j < i; j++)
                    {
                        ignored.Add(j);
                    }
                skip:
                    ;
                }
            }

            switch (mode)
            {
                case ParseMode.CreateFactory:
                    if (keywords.Count > 0)
                    {
                        ids.UnionWith(_keywords[keywords.Last()]);

                        for (int i = 0; i < keywords.Count - 1; i++)
                        {
                            HandleWord(keywords[i]);
                        }
                    }

                    foreach (string id in TaskRegistry.PriorityOrderedKeys)
                    {
                        if (ids.Contains(id))
                        {
                            _id = id;
                            Factory = GenerateFactory();

                            if (Factory.IsReady())
                            {
                                return true;
                            }
                        }
                    }

                    _id = TaskTypes.Basic;
                    Factory = TaskRegistry.BasicFactory;
                    break;
                case ParseMode.UpdateFactory:
                    foreach (var parameter in Factory.GetParameters())
                    {
                        if (!SetParameterValue(parameter))
                        {
                            ApplyParameterValue(parameter);
                        }
                    }

                    return Factory.IsReady();
                case ParseMode.UpdateValues:
                    return true;
            }

            return false;

            bool HandleWord(string word)
            {
                if (int.TryParse(word.Trim('x'), out int count) && count > 0)
                {
                    _count = count;
                }
                else if (!_settings.IgnoreNpcs && _localizedGameData.LocalizedNpcs.TryGetValue(word, out var name))
                {
                    NpcName = name;
                }
                else if (!_settings.IgnoreItems && _localizedGameData.LocalizedItems.TryGetValues(word, out var itemIds))
                {
                    ItemIds = itemIds;
                }
                else if (!_settings.IgnoreForageItems && _localizedGameData.LocalizedForageItems.TryGetValues(word, out var forageItemIds))
                {
                    ItemIds = forageItemIds;
                }
                else if (!_settings.IgnoreBuildings && _localizedGameData.LocalizedBuildings.TryGetValue(word, out var buildingType))
                {
                    BuildingType = buildingType;
                }
                else if (!_settings.IgnoreFarmAnimals && _localizedGameData.LocalizedFarmAnimals.TryGetValues(word, out var farmAnimals))
                {
                    FarmAnimals = farmAnimals;
                }
                else if (!_settings.IgnoreMachineLocations && _localizedGameData.LocalizedMachineLocations.TryGetValue(word, out var machineLoc))
                {
                    _machineLocation = machineLoc;
                }
                else if (!_settings.IgnoreLocations && _localizedGameData.LocalizedLocations.TryGetValue(word, out var locationName))
                {
                    _generalLocation = locationName;
                }
                else if (!_settings.IgnoreFarmLocations && _localizedGameData.LocalizedFarmLocations.TryGetValue(word, out var farmLoc))
                {
                    _farmLocation = farmLoc;
                }
                else if (!_settings.IgnoreForageLocations && _localizedGameData.LocalizedForageLocations.TryGetValue(word, out var forageLoc))
                {
                    _forageLocation = forageLoc;
                }
                else if (!_settings.IgnoreAnimalLocations && _localizedGameData.LocalizedAnimalLocations.TryGetValue(word, out var animalLoc))
                {
                    _animalLocation = animalLoc;
                }
                else if (!_settings.IgnoreShops && _localizedGameData.LocalizedShops.TryGetValue(word, out var shopId))
                {
                    ShopId = shopId;
                }
                else if (!_settings.IgnorePets && _localizedGameData.LocalizedPets.TryGetValue(word, out var petName))
                {
                    PetName = petName;
                }
                else if (!_settings.IgnoreMachines && _localizedGameData.LocalizedMachines.TryGetValue(word, out var machineId))
                {
                    MachineId = machineId;
                }
                else if (!_settings.IgnoreSpecialOrders && _localizedGameData.LocalizedSpecialOrder.TryGetValue(word, out var specialOrderType))
                {
                    SpecialOrderType = specialOrderType;
                }
                else if (!_settings.IgnorePassiveFestivals && _localizedGameData.LocalizedPassiveFestivals.TryGetValue(word, out var passivefestId))
                {
                    PassiveFestivalId = passivefestId;
                }
                else if (!_settings.IgnoreActiveFestivals && _localizedGameData.LocalizedActiveFestivals.TryGetValue(word, out var activeFestId))
                {
                    ActiveFestivalId = activeFestId;
                }
                else
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>Set the value of a <see cref="TaskParameter"/> based on the parser state.</summary>
        /// <param name="parameter">The parameter to update.</param>
        /// <returns>Whether the parameter value was set.</returns>
        public bool SetParameterValue(TaskParameter parameter)
        {
            Type propertyType = parameter.Type;
            TaskParameterTag tag = parameter.Attribute.Tag;

            if (tag.Equals(TaskParameterTag.NpcName) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(NpcName);
            }
            else if (tag.Equals(TaskParameterTag.Building) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(BuildingType);
            }
            else if (tag.Equals(TaskParameterTag.ItemList) && propertyType == typeof(IList<string>))
            {
                return ItemIds != null && parameter.TrySetValue(ItemIds.ToList());
            }
            else if (tag.Equals(TaskParameterTag.ForageItemList) && propertyType == typeof(IList<string>))
            {
                return ItemIds != null && parameter.TrySetValue(ItemIds.ToList());
            }
            else if (tag.Equals(TaskParameterTag.FarmAnimalList) && propertyType == typeof(IList<string>))
            {
                return FarmAnimals != null && parameter.TrySetValue(FarmAnimals.ToList());
            }
            else if (tag.Equals(TaskParameterTag.Count) && propertyType == typeof(int))
            {
                return parameter.TrySetValue(_count);
            }
            else if (tag.Equals(TaskParameterTag.Location) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(_generalLocation);
            }
            else if (tag.Equals(TaskParameterTag.FarmLocation) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(_farmLocation);
            }
            else if (tag.Equals(TaskParameterTag.ForageLocation) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(_forageLocation);
            }
            else if (tag.Equals(TaskParameterTag.AnimalLocation) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(_animalLocation);
            }
            else if (tag.Equals(TaskParameterTag.MachineLocation) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(_machineLocation);
            }
            else if (tag.Equals(TaskParameterTag.Shop) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(ShopId);
            }
            else if (tag.Equals(TaskParameterTag.PetName) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(PetName);
            }
            else if (tag.Equals(TaskParameterTag.Machine) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(MachineId);
            }
            else if (tag.Equals(TaskParameterTag.SpecialOrderType) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(SpecialOrderType);
            }
            else if (tag.Equals(TaskParameterTag.PassiveFestivalId) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(PassiveFestivalId);
            }
            else if (tag.Equals(TaskParameterTag.ActiveFestivalId) && propertyType == typeof(string))
            {
                return parameter.TrySetValue(ActiveFestivalId);
            }
            else
            {
                return false;
            }
        }

        /// <summary>Update the parser state with the value of a <see cref="TaskParameter"/>.</summary>
        /// <param name="parameter">The task parameter to use.</param>
        /// <returns>Whether the parameter value was applied.</returns>
        public bool ApplyParameterValue(TaskParameter parameter)
        {
            TaskParameterTag tag = parameter.Attribute.Tag;

            if (tag.Equals(TaskParameterTag.NpcName))
            {
                NpcName = parameter.Value is string npcName ? npcName : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.Building))
            {
                BuildingType = parameter.Value is string buildingType ? buildingType : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.ItemList))
            {
                ItemIds = parameter.Value is IList<string> itemIds ? itemIds.ToList() : null;
            }
            else if (tag.Equals(TaskParameterTag.ForageItemList))
            {
                ItemIds = parameter.Value is IList<string> l ? l.ToList() : null;
            }
            else if (tag.Equals(TaskParameterTag.FarmAnimalList))
            {
                FarmAnimals = parameter.Value is IList<string> farmAnimals ? farmAnimals.ToList() : null;
            }
            else if (tag.Equals(TaskParameterTag.Count))
            {
                _count = parameter.Value is int count ? count : 1;
            }
            else if (tag.Equals(TaskParameterTag.Location))
            {
                _generalLocation = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.FarmLocation))
            {
                _farmLocation = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.ForageLocation))
            {
                _forageLocation = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.AnimalLocation))
            {
                _animalLocation = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.MachineLocation))
            {
                _machineLocation = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.Shop))
            {
                ShopId = parameter.Value is string shopId ? shopId : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.PetName))
            {
                PetName = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.Machine))
            {
                MachineId = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.SpecialOrderType))
            {
                SpecialOrderType = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.PassiveFestivalId))
            {
                PassiveFestivalId = parameter.Value is string s ? s : string.Empty;
            }
            else if (tag.Equals(TaskParameterTag.ActiveFestivalId))
            {
                ActiveFestivalId = parameter.Value is string s ? s : string.Empty;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>Whether the smart icons for the given flags be shown.</summary>
        public bool ShouldShowSmartIcon(SmartIconFlags flags)
        {
            SmartIconFlags enabled = Factory.EnabledSmartIcons & flags;

            if (enabled.HasFlag(SmartIconFlags.Npc))
            {
                return !string.IsNullOrEmpty(NpcName);
            }
            else if (enabled.HasFlag(SmartIconFlags.Pet))
            {
                return !string.IsNullOrEmpty(PetName);
            }
            else if (enabled.HasFlag(SmartIconFlags.Building))
            {
                return !string.IsNullOrEmpty(BuildingType);
            }
            else if (enabled.HasFlag(SmartIconFlags.Item))
            {
                return ItemIds != null;
            }
            else if (enabled.HasFlag(SmartIconFlags.ForageItem))
            {
                return ItemIds != null;
            }
            else if (enabled.HasFlag(SmartIconFlags.Animal))
            {
                return FarmAnimals != null;
            }
            else if (enabled.HasFlag(SmartIconFlags.Machine))
            {
                return !string.IsNullOrEmpty(MachineId);
            }
            else if (enabled.HasFlag(SmartIconFlags.Shop))
            {
                return !string.IsNullOrEmpty(ShopId);
            }
            else if (enabled.HasFlag(SmartIconFlags.Location))
            {
                return !string.IsNullOrEmpty(_generalLocation);
            }
            else if (enabled.HasFlag(SmartIconFlags.FarmLocation))
            {
                return !string.IsNullOrEmpty(_farmLocation);
            }
            else if (enabled.HasFlag(SmartIconFlags.ForageLocation))
            {
                return !string.IsNullOrEmpty(_forageLocation);
            }
            else if (enabled.HasFlag(SmartIconFlags.AnimalLocation))
            {
                return !string.IsNullOrEmpty(_animalLocation);
            }
            else if (enabled.HasFlag(SmartIconFlags.MachineLocation))
            {
                return !string.IsNullOrEmpty(_machineLocation);
            }
            else if (enabled.HasFlag(SmartIconFlags.SpecialOrder))
            {
                return !string.IsNullOrEmpty(SpecialOrderType);
            }
            else if (enabled.HasFlag(SmartIconFlags.PassiveFestival))
            {
                return !string.IsNullOrEmpty(PassiveFestivalId);
            }
            else if (enabled.HasFlag(SmartIconFlags.ActiveFestival))
            {
                return !string.IsNullOrEmpty(ActiveFestivalId);
            }
            else
            {
                return false;
            }
        }

        public bool ShouldShowCount()
        {
            return Count > 1 && Factory.EnableSmartIconCount;
        }

        /// <summary>Generate a task.</summary>
        public ITask GenerateTask(string name)
        {
            return Factory.Create(name) ?? new BasicTask(name);
        }

        /// <summary>Generate a TaskFactory corresponding to the matched task ID.</summary>
        private TaskFactory GenerateFactory()
        {
            TaskFactory factory = TaskRegistry.CreateFactoryInstance(_id);

            foreach (var parameter in factory.GetParameters())
            {
                SetParameterValue(parameter);
            }

            return factory;
        }

        private string ResolveLocationName(string internalName)
        {
            if (string.IsNullOrEmpty(internalName)) return string.Empty;

            GameLocation? loc = Game1.getLocationFromName(internalName);
            if (loc != null)
            {
                return loc.DisplayName;
            }

            Farm farm = Game1.getFarm();
            var building = farm?.buildings.FirstOrDefault(b => b.id.Value.ToString() == internalName);

            if (building != null)
            {
                string buildingType = building.buildingType.Value;
                string displayName = buildingType;

                if (Game1.buildingData.TryGetValue(buildingType, out var data))
                {
                    displayName = TokenParser.ParseText(data.Name) ?? buildingType;
                }
                return $"{displayName} ({building.tileX.Value} {building.tileY.Value})";
            }
            return internalName;
        }

        private void PopulateKeywords(IDictionary<string, HashSet<string>> keywords)
        {
            foreach (string id in TaskRegistry.Keys)
            {
                Translation keywordList = _translation.Get("task." + id + ".keywords").UsePlaceholder(false);

                if (keywordList.HasValue())
                {
                    foreach (string keyword in keywordList.ToString().Split(','))
                    {
                        if (!keywords.ContainsKey(keyword))
                        {
                            keywords[keyword] = new HashSet<string>();
                        }

                        keywords[keyword].Add(id);
                    }
                }
            }
        }
    }
}
