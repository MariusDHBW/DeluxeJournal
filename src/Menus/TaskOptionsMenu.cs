using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

using DeluxeJournal.Framework.Task;
using DeluxeJournal.Menus.Components;
using DeluxeJournal.Task;
using DeluxeJournal.Task.Tasks;

using Period = DeluxeJournal.Task.ITask.Period;
using static StardewValley.Menus.ClickableComponent;
using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Menus
{
    /// <summary>TasksPage child menu for editing task options.</summary>
    public class TaskOptionsMenu : IClickableMenu
    {
        private const int LabelWidth = 256;
        private const int VerticalSpacing = 64;
        private const int BottomGap = 32;
        private const int InnerBorderToEdge = 32;

        private const int ParameterId = 900;
        private const int ParameterChildId = 1000;
        private const int ParameterIconId = 1100;
        private const int ColorButtonId = 500;
        
        // IDs für die Navigation
        private const int SeasonCheckboxId = 600;
        private const int WeatherCheckboxId = 700;
        private const int WeekCheckboxId = 800;
        private const int WeekdayCheckboxId = 850;

        public readonly ButtonComponent backButton;
        public readonly ButtonComponent cancelButton;
        public readonly ButtonComponent okButton;
        public readonly ButtonComponent headerButton;
        public readonly ButtonComponent expandColorsButton;
        public readonly ButtonComponent expandTypesButton;

        public readonly ClickableComponent nameTextBoxCC;
        public readonly ClickableComponent customRenewTextBoxCC;
        public readonly DropDownComponent renewPeriodDropDown;
        public readonly DropDownComponent weekdaysDropDown;
        public readonly DropDownComponent seasonsDropDown;
        public readonly DropDownComponent daysDropDown;
        
        // Checkbox Listen
        public readonly List<ClickableComponent> seasonCheckboxes;
        public readonly List<ClickableComponent> weatherCheckboxes;
        public readonly List<ClickableComponent> weekCheckboxes;
        public readonly List<ClickableComponent> weekdayCheckboxes;

        private SeasonFlags _seasonFlags; 
        private WeatherFlags _weatherFlags;
        private WeekFlags _weekFlags;
        private WeekdayFlags _weekdayFlags;

        public readonly List<ClickableComponent> typeIcons;

        private readonly SideScrollingTextBox _nameTextBox;
        private readonly SideScrollingTextBox _customRenewTextBox;
        private readonly IList<ITaskParameterComponent> _parameters;
        private readonly IDictionary<int, ITaskParameterComponent> _childParameters;
        private readonly IDictionary<int, SmartIconComponent> _parameterIcons;

        private readonly ITranslationHelper _translation;
        private readonly Texture2D _textBoxTexture;
        private readonly ITask? _task;
        private readonly int _typeIconsWidth;
        private int _typeIconsHeight;

        private Task.TaskFactory? _taskFactory;
        private DropDownComponent? _activeDropDown;
        private TaskParameterButtons? _colorButtons;
        private Rectangle _sharedContentBounds;
        private string _selectedTaskId;
        private string _hoverText;
        private int _hoverId;

        private int _taskTypeLabelY;
        private int _partitionY;

        private int _lastRenewPeriodOption = -1;

        private static bool CollapsedColorButtons { get; set; } = true;
        private static bool CollapsedTaskTypes { get; set; } = true;

        private bool ShowAdvancedOptions => (Period)renewPeriodDropDown.SelectedOption == Period.Advanced && _selectedTaskId != TaskTypes.Header;

        public TaskOptionsMenu(ITask task, ITranslationHelper translation) : this(translation)
        {
            _task = task;
            _selectedTaskId = task.ID;
            _taskFactory = TaskRegistry.CreateFactoryInstance(task.ID);
            _taskFactory.Initialize(task);

            _nameTextBox.Text = task.Name;
            renewPeriodDropDown.SelectedOption = (int)_task.RenewPeriod;
            
            _seasonFlags = _task.RequiredSeasons;
            _weatherFlags = _task.RequiredWeather;
            _weekFlags = _task.RequiredWeeks;
            _weekdayFlags = _task.RequiredWeekdays;

            if (_task.RenewPeriod == Period.Custom)
            {
                _customRenewTextBox.Text = _task.RenewCustomInterval.ToString();
            }
            else if (_task.RenewPeriod != Period.Never && _task.RenewPeriod != Period.Advanced)
            {
                weekdaysDropDown.SelectedOption = (_task.RenewDate.DayOfMonth - 1) % 7;

                if (_task.RenewPeriod != Period.Weekly)
                {
                    daysDropDown.SelectedOption = _task.RenewDate.DayOfMonth - 1;
                    seasonsDropDown.SelectedOption = _task.RenewDate.SeasonIndex;
                }
            }

            SelectTask(_selectedTaskId);
            RecalculateBounds();
        }

        public TaskOptionsMenu(string taskName, TaskParser taskParser, ITranslationHelper translation) : this(translation)
        {
            _selectedTaskId = taskParser.ID;
            _taskFactory = taskParser.Factory;
            _nameTextBox.Text = taskName;
            
            _seasonFlags = SeasonFlags.All;
            _weatherFlags = WeatherFlags.All;
            _weekFlags = WeekFlags.All;
            _weekdayFlags = WeekdayFlags.All;

            SelectTask(_selectedTaskId);
            RecalculateBounds();
        }

        private TaskOptionsMenu(ITranslationHelper translation) : base(0, 0, 928, 576)
        {
            xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
            yPositionOnScreen = ((Game1.uiViewport.Height - height) / 2) - 48;
            
            _translation = translation;
            _textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
            _activeDropDown = null;
            _taskFactory = null;
            _task = null;
            _selectedTaskId = TaskTypes.Basic;
            _hoverText = string.Empty;
            _hoverId = -1;
            exitFunction = OnExit;

            _sharedContentBounds = default;
            _sharedContentBounds.X = xPositionOnScreen + spaceToClearSideBorder + 28;
            _sharedContentBounds.Y = yPositionOnScreen + spaceToClearTopBorder + 16;
            _sharedContentBounds.Width = width - (_sharedContentBounds.X - xPositionOnScreen) * 2;
            
            _typeIconsWidth = (_sharedContentBounds.Width - LabelWidth) / 64 * 64;

            typeIcons = new List<ClickableComponent>();
            seasonCheckboxes = new List<ClickableComponent>();
            weatherCheckboxes = new List<ClickableComponent>();
            weekCheckboxes = new List<ClickableComponent>();
            weekdayCheckboxes = new List<ClickableComponent>();

            _parameters = new List<ITaskParameterComponent>();
            _parameterIcons = new Dictionary<int, SmartIconComponent>();
            _childParameters = new Dictionary<int, ITaskParameterComponent>();

            _nameTextBox = new SideScrollingTextBox(_textBoxTexture, null, Game1.smallFont, Game1.textColor)
            {
                X = _sharedContentBounds.X + LabelWidth,
                Y = _sharedContentBounds.Y,
                Width = _sharedContentBounds.Width - LabelWidth - 40
            };

            nameTextBoxCC = new ClickableComponent(_nameTextBox.Bounds, string.Empty)
            {
                myID = 100,
                downNeighborID = 101,
                rightNeighborID = 108,
                leftNeighborID = SNAP_AUTOMATIC,
                fullyImmutable = true
            };

            string[] weekdayNames = new[]
            {
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3043"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3044"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3045"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3046"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3047"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3048"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3042")
            };

            string[] seasonNames = new[]
            {
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5680"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5681"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5682"),
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5683")
            };

            string[] seasonKeys = new[]
            { 
                nameof(Season.Spring), 
                nameof(Season.Summer), 
                nameof(Season.Fall), 
                nameof(Season.Winter) 
            };

            IEnumerable<string> renewOptions = Enum.GetNames<Period>()
                .Select(period => {
                    string key = "ui.tasks.options.renew." + period;
                    return translation.Get(key).Default(period.ToString()).ToString(); 
                });

            renewPeriodDropDown = new DropDownComponent(renewOptions,
                new(_nameTextBox.X + 8, _nameTextBox.Y + VerticalSpacing, 0, 44),
                string.Empty)
            {
                myID = 101,
                upNeighborID = SNAP_AUTOMATIC,
                downNeighborID = WeatherCheckboxId, 
                rightNeighborID = SNAP_AUTOMATIC,
                leftNeighborID = SNAP_AUTOMATIC
            };

            weekdaysDropDown = new DropDownComponent(weekdayNames,
                new(renewPeriodDropDown.bounds.Right + 8, renewPeriodDropDown.bounds.Y, 0, renewPeriodDropDown.bounds.Height),
                string.Empty)
            {
                myID = 102,
                upNeighborID = SNAP_AUTOMATIC,
                downNeighborID = CUSTOM_SNAP_BEHAVIOR,
                rightNeighborID = SNAP_AUTOMATIC,
                leftNeighborID = SNAP_AUTOMATIC
            };
            
            seasonsDropDown = new DropDownComponent(seasonKeys, seasonNames,
                new(renewPeriodDropDown.bounds.Right + 8, renewPeriodDropDown.bounds.Y, 0, renewPeriodDropDown.bounds.Height),
                string.Empty)
            {
                myID = 103,
                upNeighborID = SNAP_AUTOMATIC,
                downNeighborID = CUSTOM_SNAP_BEHAVIOR,
                rightNeighborID = SNAP_AUTOMATIC,
                leftNeighborID = SNAP_AUTOMATIC
            };
            
            daysDropDown = new DropDownComponent(Enumerable.Range(1, 28).Select(i => i.ToString()),
                new(seasonsDropDown.bounds.Right + 8, seasonsDropDown.bounds.Y, 112, seasonsDropDown.bounds.Height),
                string.Empty,
                DropDownComponent.WrapStyle.Horizontal,
                wrapLimit: 7,
                fixedWidth: true)
            {
                myID = 104,
                upNeighborID = SNAP_AUTOMATIC,
                downNeighborID = CUSTOM_SNAP_BEHAVIOR,
                rightNeighborID = SNAP_AUTOMATIC,
                leftNeighborID = SNAP_AUTOMATIC
            };

            _customRenewTextBox = new SideScrollingTextBox(_textBoxTexture, null, Game1.smallFont, Game1.textColor)
            {
                X = weekdaysDropDown.bounds.X,
                Y = weekdaysDropDown.bounds.Y,
                Width = _sharedContentBounds.Right - weekdaysDropDown.bounds.X,
                numbersOnly = true
            };

            customRenewTextBoxCC = new ClickableComponent(_customRenewTextBox.Bounds, string.Empty)
            {
                myID = 105,
                upNeighborID = SNAP_AUTOMATIC,
                downNeighborID = CUSTOM_SNAP_BEHAVIOR,
                rightNeighborID = SNAP_AUTOMATIC,
                leftNeighborID = SNAP_AUTOMATIC
            };

            // --- ADVANCED CHECKBOXES INITIALISIEREN ---
            // 1. Seasons
            for (int i = 0; i < 4; i++)
            {
                seasonCheckboxes.Add(new ClickableComponent(new Rectangle(0,0,1,1), i.ToString()) 
                { myID = SeasonCheckboxId + i, label = seasonNames[i] });
            }
            
            // 2. Weather
            string[] weatherKeys = ["sun", "rain"]; 
            for (int i = 0; i < 2; i++)
            {
                weatherCheckboxes.Add(new ClickableComponent(new Rectangle(0,0,1,1), i.ToString()) 
                { myID = WeatherCheckboxId + i, label = translation.Get("ui.tasks.options.weather." + weatherKeys[i]).Default(weatherKeys[i]).ToString() });
            }

            // 3. Weeks
            for (int i = 0; i < 4; i++)
            {
                weekCheckboxes.Add(new ClickableComponent(new Rectangle(0,0,1,1), i.ToString()) 
                { myID = WeekCheckboxId + i, label = (i + 1).ToString() });
            }

            // 4. Weekdays
            string[] dayShortKeys = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"];
            for (int i = 0; i < 7; i++)
            {
                string shortName = translation.Get("ui.days.short." + dayShortKeys[i]).Default(weekdayNames[i][..3]).ToString();
                weekdayCheckboxes.Add(new ClickableComponent(new Rectangle(0,0,1,1), i.ToString()) 
                { myID = WeekdayCheckboxId + i, label = shortName });
            }

            Rectangle iconBounds = new Rectangle(0, 0, 56, 56);
            int snapId = 0;

            foreach (string id in TaskRegistry.Keys)
            {
                if (id == TaskTypes.Header) 
                {
                    continue;
                }
                typeIcons.Add(new ClickableComponent(iconBounds, id)
                {
                    myID = snapId,
                    upNeighborID = SNAP_AUTOMATIC,
                    downNeighborID = CUSTOM_SNAP_BEHAVIOR,
                    rightNeighborID = snapId == TaskRegistry.Count - 1 ? 106 : snapId + 1,
                    leftNeighborID = snapId == 0 ? 105 : snapId - 1,
                    fullyImmutable = true
                });
                snapId++;
            }

            backButton = new ButtonComponent(
                new Rectangle(xPositionOnScreen - 64, _sharedContentBounds.Y + 12, 64, 32), 
                Game1.mouseCursors, 
                new Rectangle(352, 495, 12, 11), 
                4f)
            { 
                myID = 105, 
                downNeighborID = 0, 
                rightNeighborID = nameTextBoxCC.myID, 
                fullyImmutable = true 
            };

            cancelButton = new ButtonComponent(
                new Rectangle(xPositionOnScreen + width - 12, _sharedContentBounds.Y, 64, 64), 
                DeluxeJournalMod.UiTexture!, 
                new Rectangle(0, 80, 16, 16), 
                4f)
            { 
                myID = 106, 
                downNeighborID = 109, 
                leftNeighborID = nameTextBoxCC.myID, 
                fullyImmutable = true, 
                hoverText = translation.Get("ui.tasks.new.cancelbutton.hover") 
            };

            okButton = new ButtonComponent(
                new Rectangle(xPositionOnScreen + width - 100, yPositionOnScreen + height - 4, 64, 64), 
                DeluxeJournalMod.UiTexture!, 
                new Rectangle(32, 80, 16, 16), 
                4f)
            {
                myID = 107, 
                upNeighborID = CUSTOM_SNAP_BEHAVIOR, 
                rightNeighborID = cancelButton.myID, 
                fullyImmutable = true 
            };

            headerButton = new ButtonComponent(
                new Rectangle(_sharedContentBounds.Right - 44, _sharedContentBounds.Y, 44, 48), 
                DeluxeJournalMod.UiTexture!, 
                new Rectangle(16, 52, 11, 12), 
                4f)
            { 
                myID = 108, 
                downNeighborID = renewPeriodDropDown.myID, 
                rightNeighborID = SNAP_AUTOMATIC, 
                leftNeighborID = nameTextBoxCC.myID, 
                fullyImmutable = true, 
                hoverText = translation.Get("task.header"), 
                SoundCueName = "drumkit6" 
            };

            expandColorsButton = new ButtonComponent(
                new(xPositionOnScreen + width - spaceToClearSideBorder + 8, 0, 32, 48), 
                DeluxeJournalMod.UiTexture!, 
                new(64, 51, 8, 12), 4f, 
                true)
            { 
                myID = 109, 
                downNeighborID = okButton.myID, 
                upNeighborID = cancelButton.myID, 
                leftNeighborID = SNAP_AUTOMATIC, 
                downNeighborImmutable = true, 
                upNeighborImmutable = true, 
                ShadowIntensity = 0.2f 
            };

            expandTypesButton = new ButtonComponent(
                new(xPositionOnScreen + width - spaceToClearSideBorder + 8, 0, 32, 48), 
                DeluxeJournalMod.UiTexture!, 
                new(64, 51, 8, 12), 
                4f, 
                true)
            { 
                myID = 110, 
                downNeighborID = okButton.myID, 
                upNeighborID = cancelButton.myID, 
                leftNeighborID = SNAP_AUTOMATIC, 
                downNeighborImmutable = true, 
                upNeighborImmutable = true, 
                ShadowIntensity = 0.2f 
            };

            Game1.playSound("shwip");
        }

        public void RecalculateBounds()
        {
            int currentY = _sharedContentBounds.Y + VerticalSpacing * 2; 

            // --- ADVANCED OPTIONS LAYOUT ---
            if (ShowAdvancedOptions)
            {
                int startX = renewPeriodDropDown.bounds.X;
                int availableWidth = _sharedContentBounds.Right - startX - 16;
                int fixedColumnWidth = availableWidth / 4;
                currentY = LayoutCheckBoxGroup(weatherCheckboxes, startX, currentY, 4, fixedColumnWidth);
                currentY = LayoutCheckBoxGroup(weekCheckboxes, startX, currentY, 4, fixedColumnWidth);
                currentY = LayoutCheckBoxGroup(weekdayCheckboxes, startX, currentY, 4, fixedColumnWidth);
                currentY = LayoutCheckBoxGroup(seasonCheckboxes, startX, currentY, 4, fixedColumnWidth);
                currentY += 16; 
            }

            // Farben
            if (_colorButtons != null)
            {
                _colorButtons.bounds.Y = currentY;
                _colorButtons.Collapsed = CollapsedColorButtons;
                expandColorsButton.bounds.Y = currentY - 4;
                
                _colorButtons.RecalculateBounds();
                currentY = _colorButtons.bounds.Bottom + 12;
            }

            // Parameter & Icons
            if (_taskFactory != null)
            {
                for (int i = 0; i < _parameters.Count; i++)
                {
                    var parameter = _parameters[i];
                    
                    parameter.ClickableComponent.bounds.Y = currentY;
                    parameter.RecalculateBounds();

                    if (_parameterIcons.TryGetValue(i, out var icon))
                    {
                        icon.Location = new Point(parameter.ClickableComponent.bounds.X - 60, currentY - 4);
                    }
                    if (_childParameters.TryGetValue(i, out var child))
                    {
                        child.ClickableComponent.bounds.Y = currentY;
                        child.RecalculateBounds();
                    }
                    currentY += VerticalSpacing;
                }
            }
            
            // Trennlinie & Label
            currentY += 8;
            _partitionY = currentY; 
            _taskTypeLabelY = _partitionY + 48; 
            currentY = _taskTypeLabelY + 8; 

            // Task Icons
            expandTypesButton.bounds.Y = _taskTypeLabelY + 4;

            int iconsRows = (typeIcons.Count + (_typeIconsWidth/64) - 1) / (_typeIconsWidth/64);
            if (CollapsedTaskTypes) iconsRows = 1;
            
            _typeIconsHeight = iconsRows * VerticalSpacing + 4;

            for (int i = 0; i < typeIcons.Count; i++)
            {
                int row = i * 64 / _typeIconsWidth;
                int col = i * 64 % _typeIconsWidth;
                
                typeIcons[i].bounds.X = _sharedContentBounds.X + LabelWidth + 12 + col;
                typeIcons[i].bounds.Y = currentY + row * VerticalSpacing;
            }

            currentY += _typeIconsHeight;
            _sharedContentBounds.Height = currentY - _sharedContentBounds.Y;

            height = _sharedContentBounds.Bottom + BottomGap - yPositionOnScreen;
            okButton.bounds.Y = yPositionOnScreen + height - 4;
        }

        private static int LayoutCheckBoxGroup(IEnumerable<ClickableComponent> checkboxes, int startX, int startY, int itemsPerRow, int fixedColumnWidth)
        {
            int x = startX;
            int y = startY;
            int count = 0;
            int rowHeight = 40;

            foreach (var cb in checkboxes)
            {
                if (count >= itemsPerRow)
                {
                    x = startX;
                    y += rowHeight;
                    count = 0;
                }
                Vector2 size = Game1.smallFont.MeasureString(cb.label);
                cb.bounds = new Rectangle(x, y, 36 + (int)size.X, 36);
                x += fixedColumnWidth;
                count++;
            }
            return y + rowHeight;
        }

        public bool CanApplyChanges()
        {
            return _nameTextBox.Text.Trim().Length > 0 && _taskFactory?.IsReady() == true;
        }

        private void ApplyChanges()
        {
            string name = _nameTextBox.Text.Trim();
            string season = (seasonsDropDown.SelectedOption >= 0 && seasonsDropDown.SelectedOption < seasonsDropDown.Options.Count) 
                            ? seasonsDropDown.Options[seasonsDropDown.SelectedOption] 
                            : "spring";

            ITask task = _taskFactory?.Create(name) ?? new BasicTask(name);
            TasksPage? tasksPage = null;

            task.RenewPeriod = (Period)renewPeriodDropDown.SelectedOption;
            task.RenewCustomInterval = task.RenewPeriod == Period.Custom && int.TryParse(_customRenewTextBox.Text, out int days) ? days : 1;
            
            if (task.RenewPeriod != Period.Advanced)
            {
                task.RenewDate = new WorldDate(1, season, task.RenewPeriod switch
                {
                    Period.Custom => 1,
                    Period.Weekly => weekdaysDropDown.SelectedOption + 1,
                    _ => daysDropDown.SelectedOption + 1
                });

                task.RequiredSeasons = SeasonFlags.All;
                task.RequiredWeather = WeatherFlags.All;
                task.RequiredWeeks = WeekFlags.All;
                task.RequiredWeekdays = WeekdayFlags.All;
            }
            else
            {
                task.RequiredSeasons = _seasonFlags;
                task.RequiredWeather = _weatherFlags;
                task.RequiredWeeks = _weekFlags;
                task.RequiredWeekdays = _weekdayFlags;
                task.RenewDate = WorldDate.Now(); 
            }

            if (task.DaysRemaining() > 0)
            {
                task.Active = false;
            }

            for (IClickableMenu parent = GetParentMenu(); parent != null; parent = parent.GetParentMenu())
            {
                if (parent is TasksPage) 
                { 
                    tasksPage = parent as TasksPage; 
                    break; 
                }
            }

            if (_task != null)
            {
                if (DeluxeJournalMod.TaskManager is TaskManager taskManager)
                {
                    IList<ITask> tasks = taskManager.Tasks;
                    int index = tasks.IndexOf(_task);

                    task.Active = _task.Active;
                    task.Complete = _task.Complete;
                    task.Count = _task.Count;
                    task.MarkAsViewed();
                    task.SetSortingIndex(_task.GetSortingIndex());

                    if (task.Active && task.Count >= task.MaxCount) 
                    {
                        task.Complete = true;
                    }

                    task.Validate();
                    tasks.RemoveAt(index);
                    tasks.Insert(index, task);
                    tasksPage?.OnTasksUpdated();
                }
            }
            else
            {
                tasksPage?.AddTask(task);
            }
        }

        public void ToggleColorButtons() 
        { 
            CollapsedColorButtons = !CollapsedColorButtons; 
            RecalculateBounds(); 
        }
        public void ToggleTaskTypes() 
        { 
            CollapsedTaskTypes = !CollapsedTaskTypes; 
            RecalculateBounds(); 
        }

        public void SelectTask(string id)
        {
            bool renewable = id != TaskTypes.Header;

            if (_selectedTaskId != id)
            {
                int colorIndex = _taskFactory?.ColorIndex ?? 0;

                _selectedTaskId = id;
                _taskFactory = TaskRegistry.CreateFactoryInstance(id);
                _taskFactory.ColorIndex = colorIndex;
            }

            renewPeriodDropDown.Active = renewable;
            weekdaysDropDown.Active = renewable;
            seasonsDropDown.Active = renewable;
            daysDropDown.Active = renewable;

            SetupParameters();
        }

        private void SetupParameters()
        {
            _parameterIcons.Clear();
            _parameters.Clear();
            _childParameters.Clear();
            _colorButtons = null;

            if (_taskFactory != null)
            {
                IReadOnlyList<TaskParameter> parameters = _taskFactory.GetParameters();

                for (int i = 0, j = 0; i < parameters.Count; i++)
                {
                    TaskParameter parameter = parameters[i];
                    Rectangle parameterBounds = new Rectangle(
                        _sharedContentBounds.X + LabelWidth, 
                        0, 
                        _sharedContentBounds.Width - LabelWidth, 
                        _textBoxTexture.Height);

                    if (parameter.Attribute.Hidden) 
                    {
                        continue;
                    }
                    else if (parameter.Attribute.Parent is string parentName)
                    {
                        for (int k = 0; k < _parameters.Count; k++)
                        {
                            TaskParameter parentParameter = _parameters[k].Parameter;

                            if (parentParameter.Attribute.Name == parentName)
                            {
                                if (_parameters[k] is not TaskParameterTextBox parentTextBox || parameter.Attribute.InputType != TaskParameterInputType.DropDown) 
                                {
                                    break;
                                }

                                ClickableComponent parentTextBoxCC = parentTextBox.ClickableComponent;
                                Rectangle dropDownBounds = new Rectangle(parentTextBox.X + parentTextBox.Width - 120, parentTextBox.Y, 120, 44);
                                parentTextBox.Width = _sharedContentBounds.Width - LabelWidth - dropDownBounds.Width + 4;
                                parentTextBoxCC.bounds.Width = parentTextBox.Width;

                                if (parameter.Attribute.Tag == TaskParameterTag.Quality && DeluxeJournalMod.UiTexture is Texture2D uiTexture)
                                {
                                    KeyValuePair<Texture2D, Rectangle>[] options = new KeyValuePair<Texture2D, Rectangle>[]
                                    {
                                        new(uiTexture, new Rectangle(112, 80, 8, 8)),
                                        new(Game1.mouseCursors, new Rectangle(338, 400, 8, 8)),
                                        new(Game1.mouseCursors, new Rectangle(346, 400, 8, 8)),
                                        new(Game1.mouseCursors, new Rectangle(346, 392, 8, 8))
                                    };

                                    _childParameters.Add(k, new TaskParameterDropDown(parameter, options, dropDownBounds)
                                    {
                                        myID = ParameterChildId + k,
                                        upNeighborID = parentTextBoxCC.upNeighborID,
                                        downNeighborID = parentTextBoxCC.downNeighborID,
                                        rightNeighborID = parentTextBoxCC.rightNeighborID,
                                        leftNeighborID = parentTextBoxCC.myID,
                                        fullyImmutable = true
                                    });

                                    parentTextBoxCC.rightNeighborID = ParameterChildId + k;
                                }
                                break;
                            }
                        }
                        continue;
                    }
                    else if (parameter.Attribute.InputType == TaskParameterInputType.ColorButtons)
                    {
                        List<ButtonComponent> buttons = new();
                        Rectangle btnBounds = new Rectangle(parameterBounds.X + 16, 0, parameterBounds.Width, parameterBounds.Height);

                        for (int k = 0; k < DeluxeJournalMod.ColorSchemas.Count; k++)
                        {
                            buttons.Add(new ColorButtonComponent(k.ToString(), new(0, 0, 48, 48), k) 
                            { 
                                myID = ColorButtonId + k, 
                                fullyImmutable = true 
                            });
                        }

                        _colorButtons = new TaskParameterButtons(parameter, buttons, btnBounds, 16, CollapsedColorButtons)
                        {
                            upNeighborID = SNAP_AUTOMATIC,
                            downNeighborID = CUSTOM_SNAP_BEHAVIOR,
                            rightNeighborID = cancelButton.myID,
                            leftNeighborID = backButton.myID,
                            fullyImmutable = true,
                            Label = _translation.Get("ui.tasks.parameter.color").Default(parameter.Attribute.Name)
                        };

                        continue;
                    }
                    else if (parameter.Attribute.InputType == TaskParameterInputType.TextBox)
                    {
                        Rectangle iconBounds = new Rectangle(parameterBounds.X - 60, parameterBounds.Y - 4, 56, 56);

                        ClickableComponent textBoxCC = new(parameterBounds, parameter.Attribute.Name)
                        {
                            myID = ParameterId + j,
                            upNeighborID = j == 0 ? CUSTOM_SNAP_BEHAVIOR : ParameterId + j - 1,
                            downNeighborID = ParameterId + j + 1,
                            rightNeighborID = cancelButton.myID,
                            leftNeighborID = CUSTOM_SNAP_BEHAVIOR,
                            fullyImmutable = true
                        };

                        TaskParameterTextBox textBox = new TaskParameterTextBox(parameter, _taskFactory, textBoxCC, _textBoxTexture, null, Game1.smallFont, Game1.textColor, _translation)
                        {
                            X = parameterBounds.X,
                            Y = parameterBounds.Y,
                            Width = parameterBounds.Width,
                            Label = _translation.Get("ui.tasks.parameter." + parameter.Attribute.Name).Default(parameter.Attribute.Name),
                            numbersOnly = parameter.Type == typeof(int)
                        };

                        SmartIconFlags mask = _taskFactory.EnabledSmartIcons & parameter.Attribute.Tag switch
                        {
                            TaskParameterTag.ItemList => SmartIconFlags.Item,
                            TaskParameterTag.ForageItemList => SmartIconFlags.ForageItem, 
                            TaskParameterTag.Building => SmartIconFlags.Building,
                            TaskParameterTag.FarmAnimalList => SmartIconFlags.Animal,
                            TaskParameterTag.NpcName => SmartIconFlags.Npc,
                            TaskParameterTag.Location => SmartIconFlags.Location,
                            TaskParameterTag.FarmLocation => SmartIconFlags.FarmLocation, 
                            TaskParameterTag.ForageLocation => SmartIconFlags.ForageLocation, 
                            TaskParameterTag.MachineLocation => SmartIconFlags.MachineLocation,
                            TaskParameterTag.AnimalLocation => SmartIconFlags.AnimalLocation,
                            TaskParameterTag.Shop => SmartIconFlags.Shop,
                            TaskParameterTag.PetName => SmartIconFlags.Pet, 
                            TaskParameterTag.Machine => SmartIconFlags.Machine, 
                            TaskParameterTag.SpecialOrderType => SmartIconFlags.SpecialOrder,
                            TaskParameterTag.PassiveFestivalId => SmartIconFlags.PassiveFestival,
                            TaskParameterTag.ActiveFestivalId => SmartIconFlags.ActiveFestival,
                            _ => SmartIconFlags.None
                        };

                        if (mask != SmartIconFlags.None)
                        {
                            _parameterIcons.Add(j, new SmartIconComponent(iconBounds, textBox.Parser, ParameterIconId + j, mask, 1, false));
                        }

                        _parameters.Add(textBox);
                    }

                    j++;
                }
                
                if (_parameters.LastOrDefault() is ITaskParameterComponent lastParameter)
                {
                    lastParameter.ClickableComponent.downNeighborID = okButton.myID;

                    if (lastParameter is TaskParameterButtons parameterButtons)
                    {
                        parameterButtons.RemapButtonNeighbors();
                    }
                }
            }

            RecalculateBounds();
            populateClickableComponentList();
        }
        
        protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
        {
            switch (direction)
            {
                case Game1.up:
                   if (oldID >= SeasonCheckboxId && oldID < 1000) 
                   {
                        currentlySnappedComponent = renewPeriodDropDown;
                   }
                   else if (oldID == okButton.myID && _parameters.Count > 0)
                   {
                        currentlySnappedComponent = _parameters.Last().GetClickableComponents().First();
                   }
                   else
                   {
                        currentlySnappedComponent = GetSelectedTypeIcon(_selectedTaskId);
                   }
                   break;
            }
            base.customSnapBehavior(direction, oldRegion, oldID);
        }
        
        public ClickableComponent? GetSelectedTypeIcon(string name) 
        { 
            foreach (ClickableComponent component in typeIcons) 
            {
            if (component.name == name) 
            {
                return component; 
            }
            }

            return null; 
        }
        
        public override void populateClickableComponentList()
        {
            base.populateClickableComponentList();

            foreach (var icon in _parameterIcons.Values) 
            {
                allClickableComponents.AddRange(icon.GetClickableComponents());
            }

            foreach (var parameter in _parameters.Concat(_childParameters.Values)) 
            {
                allClickableComponents.AddRange(parameter.GetClickableComponents());
            }
            allClickableComponents.AddRange(seasonCheckboxes);
            allClickableComponents.AddRange(weatherCheckboxes);
            allClickableComponents.AddRange(weekCheckboxes);
            allClickableComponents.AddRange(weekdayCheckboxes);
        }
        
        public override void snapCursorToCurrentSnappedComponent()
        {
            base.snapCursorToCurrentSnappedComponent();

            if (currentlySnappedComponent is ClickableComponent cc 
                && cc.myID >= ParameterId 
                && cc.myID < ParameterId + _parameters.Count)
            {
                (_parameters[cc.myID - ParameterId] as TaskParameterTextBox)?.FillWithParsedText();
            }
        }

        public override void snapToDefaultClickableComponent() 
        { 
            currentlySnappedComponent = backButton; 
            snapCursorToCurrentSnappedComponent(); 
        }

        public override void receiveGamePadButton(Buttons b) 
        { 
            switch (b) 
            { 
                case Buttons.B:
                    currentlySnappedComponent = cancelButton; 
                    snapCursorToCurrentSnappedComponent(); 
                    break;
            } 
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (Game1.keyboardDispatcher.Subscriber is IKeyboardSubscriber keyboard)
            {
                if (keyboard is TaskParameterTextBox activeTextBox) 
                {
                    activeTextBox.FillWithParsedText();
                }

                    keyboard.Selected = false;
            }

            if (ShowAdvancedOptions)
            {
                if (TryToggleFlag(weatherCheckboxes, x, y, i => i == 0 ? WeatherFlags.Sun : WeatherFlags.Rain, ref _weatherFlags)) return;
                if (TryToggleFlag(weekCheckboxes, x, y, i => (WeekFlags)(1 << i), ref _weekFlags)) return;
                if (TryToggleFlag(weekdayCheckboxes, x, y, i => (WeekdayFlags)(1 << i), ref _weekdayFlags)) return;
                if (TryToggleFlag(seasonCheckboxes, x, y, i => (SeasonFlags)(1 << i), ref _seasonFlags)) return;
            }

            if (expandTypesButton.containsPoint(x, y)) 
            { 
                expandTypesButton.ReceiveLeftClick(x, y, playSound); 
                ToggleTaskTypes(); 
                return; 
            }
            if (backButton.containsPoint(x, y)) 
            {
                exitThisMenu(playSound);
            }
            else if (cancelButton.containsPoint(x, y)) 
            {
                ExitAllChildMenus(playSound);
            }
            else if (okButton.containsPoint(x, y) && CanApplyChanges()) 
            {
                ApplyChangesAndExit(playSound);
            }
            else if (headerButton.containsPoint(x, y)) 
            { 
                headerButton.ReceiveLeftClick(x, y, playSound); 
                SelectTask(_selectedTaskId == TaskTypes.Header ? TaskTypes.Basic : TaskTypes.Header); 
            }
            else if (expandColorsButton.containsPoint(x, y)) 
            { 
                expandColorsButton.ReceiveLeftClick(x, y, playSound); 
                ToggleColorButtons(); 
            }
            else if (nameTextBoxCC.containsPoint(x, y)) 
            {
                 _nameTextBox.SelectMe(); 
                 _nameTextBox.ForceUpdate(); 
            }
            else if (renewPeriodDropDown.containsPoint(x, y)) 
            { 
                renewPeriodDropDown.ReceiveLeftClick(x, y); 
                _activeDropDown = renewPeriodDropDown; 
            }
            else if (weekdaysDropDown.containsPoint(x, y)) 
            { 
                weekdaysDropDown.ReceiveLeftClick(x, y); 
                _activeDropDown = weekdaysDropDown; 
            }
            else if (seasonsDropDown.containsPoint(x, y)) 
            { 
                seasonsDropDown.ReceiveLeftClick(x, y); 
                _activeDropDown = seasonsDropDown; 
            }
            else if (daysDropDown.containsPoint(x, y)) 
            { 
                daysDropDown.ReceiveLeftClick(x, y); 
                _activeDropDown = daysDropDown; 
            }
            else if (customRenewTextBoxCC.containsPoint(x, y)) 
            { 
                _customRenewTextBox.SelectMe(); 
                _customRenewTextBox.ForceUpdate(); 
            }
            else if (_colorButtons?.containsPoint(x, y) == true)
            {
             _colorButtons.ReceiveLeftClick(x, y);
            }
            else
            {
                for (int i = 0; i < typeIcons.Count; i++) 
                {
                    int row = i * 64 / _typeIconsWidth;
                    if (CollapsedTaskTypes && row > 0) continue;
                    if (typeIcons[i].containsPoint(x, y)) 
                    { 
                        if (playSound) 
                        {
                            Game1.playSound("drumkit6"); 
                        }

                        SelectTask(typeIcons[i].name); 
                        return; 
                    }
                }

                for (int i = 0; i < _parameters.Count; i++) 
                {
                    ITaskParameterComponent parameter = _parameters[i];

                    if (parameter.ClickableComponent.containsPoint(x, y)) 
                    {
                        parameter.ReceiveLeftClick(x, y);
                    }
                    else if (_childParameters.TryGetValue(i, out var child) && child.ClickableComponent.containsPoint(x, y)) 
                    { 
                        child.ReceiveLeftClick(x, y); 

                        if (child is TaskParameterDropDown dropDown) 
                        {
                            _activeDropDown = dropDown; 
                        }
                    }
                    else 
                    {
                        continue; 
                    }

                    return;
                }
            }
        }

        private static bool TryToggleFlag<TEnum>(IList<ClickableComponent> checkboxes, int x, int y, Func<int, TEnum> flagSelector, ref TEnum flags) where TEnum : struct, Enum
        {
            for (int i = 0; i < checkboxes.Count; i++)
            {
                if (checkboxes[i].containsPoint(x, y))
                {
                    Game1.playSound("drumkit6");
                    TEnum flag = flagSelector(i);
                    
                    int currentValues = Convert.ToInt32(flags);
                    int flagValue = Convert.ToInt32(flag);
                    
                    flags = (TEnum)Enum.ToObject(typeof(TEnum), currentValues ^ flagValue);
                    return true;
                }
            }
            return false;
        }

        public override void leftClickHeld(int x, int y) 
        { 
            if (_activeDropDown != null) 
            {
                _activeDropDown.LeftClickHeld(x, y); 
            }
        }

        public override void releaseLeftClick(int x, int y) 
        { 
            if (_activeDropDown != null) 
            { 
                _activeDropDown.LeftClickReleased(x, y); 
                _activeDropDown = null; 
            }
        }

        public override void receiveKeyPress(Keys key) 
        { 
            if (Game1.options.SnappyMenus) 
            {
                applyMovementKey(key);
            } 
            else if (Game1.keyboardDispatcher.Subscriber == null) 
            {
                base.receiveKeyPress(key); 
            }

            if (_activeDropDown != null)
            {
                _activeDropDown.ReceiveKeyPress(key); 
            }

            switch (key) 
            { 
                case Keys.Escape: 
                    exitThisMenu(); 
                    break; 
                case Keys.Tab:
                    CycleTextBoxes(); 
                    break; 
                case Keys.Enter: 
                    if (CycleTextBoxes() && CanApplyChanges()) 
                    {
                        ApplyChangesAndExit(); 
                    }
                    break;
                }
        }

        /// <summary>Cycle through text boxes.</summary>
        /// <param name="completeParameterText">Complete the parameter text before attempting to cycle.</param>
        /// <returns><c>true</c> if the next text box was selected and <c>false</c> otherwise.</returns>
        private bool CycleTextBoxes(bool completeParameterText = true)
        {
            if (_activeDropDown != null || Game1.options.SnappyMenus) 
            {
                return false;
            }

            TextBox? selectTextBox = _nameTextBox;

            if (_nameTextBox.Selected)
            {
                if (customRenewTextBoxCC.visible) 
                {
                    selectTextBox = _customRenewTextBox;
                }
                else if (_parameters.Count > 0) 
                {
                    selectTextBox = _parameters.FirstOrDefault(parameter => parameter is TextBox) as TextBox;
                }
            }
            else if (_customRenewTextBox.Selected) 
            {
                selectTextBox = _parameters.FirstOrDefault(parameter => parameter is TextBox) as TextBox;
            }
            else if (_parameters.Count > 0)
            {
                for (int i = 0; i < _parameters.Count; i++)
                {
                    if (_parameters[i] is TaskParameterTextBox textBox && textBox.Selected)
                    {
                        if (completeParameterText && textBox.FillWithParsedText()) 
                        {
                            return false;
                        }
                        else if (_parameters.Skip(i + 1).FirstOrDefault(parameter => parameter is TextBox) is TextBox nextTextBox) 
                        {
                            selectTextBox = nextTextBox;
                        }

                        break;
                    }
                }
            }

            if (selectTextBox?.Selected == false) 
            {
                selectTextBox.SelectMe();
            }

            return true;
        }

        public override void applyMovementKey(int direction) 
        { 
            if (_activeDropDown == null) 
            {
                base.applyMovementKey(direction); 
            }
        }

        public override void performHoverAction(int x, int y)
        {
            _hoverText = string.Empty;
            _hoverId = -1;

            if (_activeDropDown != null) 
            {
                return;
            }

            if (cancelButton.containsPoint(x, y)) 
            {
                _hoverText = cancelButton.hoverText;
            }
            else if (headerButton.containsPoint(x, y)) 
            {
                _hoverText = headerButton.hoverText; 
                _hoverId = headerButton.myID; 
            }
            else
            {
                for (int i = 0; i < typeIcons.Count; i++)
                {
                    int row = i * 64 / _typeIconsWidth;
                    if (CollapsedTaskTypes && row > 0) 
                    {
                        continue;
                        }
                    if (typeIcons[i].containsPoint(x, y)) 
                    { 
                        _hoverText = _translation.Get("task." + typeIcons[i].name); 
                        break; 
                    }
                }
            }
            
            if(ShowAdvancedOptions)
            {
                foreach(var list in new[]{seasonCheckboxes, weatherCheckboxes, weekCheckboxes, weekdayCheckboxes})
                    foreach(var cb in list) if (cb.containsPoint(x, y)) _hoverText = cb.label;
            }

            if (_taskFactory != null)
            {
                bool foundHoverText = !string.IsNullOrEmpty(_hoverText);

                for (int i = 0; i < _parameters.Count; i++)
                {
                    _parameters[i].TryHover(x, y);

                    if (!foundHoverText)
                    {
                        if (_parameterIcons.TryGetValue(i, out var icon) && icon.TryGetHoverText(x, y, _translation, out string hoverText)) 
                        {
                            _hoverText = hoverText;
                        }
                        else if (_childParameters.TryGetValue(i, out var child) && child.ClickableComponent.containsPoint(x, y)) 
                        {
                            _hoverText = _translation.Get("ui.tasks.parameter." + child.Parameter.Attribute.Name);
                        }
                        else 
                        {
                            continue;
                        }

                        foundHoverText = true;
                    }
                }
            }

            _colorButtons?.TryHover(x, y);
            backButton.tryHover(x, y, 0.2f);
            cancelButton.tryHover(x, y, 0.2f);
            okButton.tryHover(x, y);
            expandColorsButton.tryHover(x, y, 0.14f);
            expandTypesButton.tryHover(x, y, 0.14f);
        }

        public override void draw(SpriteBatch b)
        {
            if (renewPeriodDropDown.SelectedOption != _lastRenewPeriodOption)
            {
                _lastRenewPeriodOption = renewPeriodDropDown.SelectedOption;
                RecalculateBounds(); 
            }

            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
            SpriteText.drawStringWithScrollCenteredAt(b, _translation.Get("ui.tasks.options"), xPositionOnScreen + width / 2, yPositionOnScreen + 16);
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);
            drawHorizontalPartition(b, _partitionY);

            if (DeluxeJournalMod.ColorSchemas.Count > 9 && _colorButtons != null)
            {
                DrawRightButtonBox(b, new(expandColorsButton.bounds.X - 8, expandColorsButton.bounds.Y - InnerBorderToEdge - 12, InnerBorderToEdge + 44, VerticalSpacing + InnerBorderToEdge * 2 + 8), Color.White);
                expandColorsButton.draw(b, Color.White, 0.88f, CollapsedColorButtons ? 0 : 1);
            }
            if (typeIcons.Count * 64 > _typeIconsWidth)
            {
                 DrawRightButtonBox(b, new(expandTypesButton.bounds.X - 8, expandTypesButton.bounds.Y - InnerBorderToEdge - 12, InnerBorderToEdge + 44, VerticalSpacing + InnerBorderToEdge * 2 + 8), Color.White);
                 expandTypesButton.draw(b, Color.White, 0.88f, CollapsedTaskTypes ? 0 : 1);
            }

            DrawLabel(b, _translation.Get("ui.tasks.options.type"), _taskTypeLabelY, Game1.textColor);

            for (int i = 0; i < typeIcons.Count; i++)
            {
                int row = i * 64 / _typeIconsWidth;
                if (CollapsedTaskTypes && row > 0) continue; 
                TaskRegistry.GetTaskIcon(typeIcons[i].name).DrawIcon(b, 
                typeIcons[i].bounds, 
                typeIcons[i].name == _selectedTaskId ? Color.White : Color.DimGray, 
                drawShadow: true);
            }

            if (_colorButtons is TaskParameterButtons colorButtons)
            {
                DrawLabel(b, colorButtons.Label, colorButtons.bounds.Y, Game1.textColor);
                colorButtons.Draw(b);
            }

            backButton.draw(b);
            cancelButton.draw(b);
            okButton.draw(b, CanApplyChanges() ? Color.White : Color.Gray * 0.8f, 0.88f);

            if (_taskFactory != null)
            {
                for (int i = _parameters.Count - 1; i >= 0; i--)
                {
                    ITaskParameterComponent parameter = _parameters[i];
                    int quality = 0;

                    DrawLabel(b, parameter.Label, parameter.ClickableComponent.bounds.Y, parameter.Parameter.IsValid() ? Game1.textColor : Color.DarkRed);

                    if (_childParameters.TryGetValue(i, out var child))
                    {
                        child.Draw(b);

                        if (child.Parameter.Attribute.Tag == TaskParameterTag.Quality && child.Parameter.Value is int value) 
                        {
                            quality = value;
                        }
                    }

                    if (_parameterIcons.TryGetValue(i, out var icon)) 
                    {
                        icon.Draw(b, parameter.Parameter.IsValid() ? Color.White : Color.Gray * 0.8f, quality, false, true);
                    }

                    parameter.Draw(b);
                }
            }

            Period renewPeriod = (Period)renewPeriodDropDown.SelectedOption;
            bool isHeader = _selectedTaskId == TaskTypes.Header;

            DrawLabel(b, 
            isHeader ? headerButton.hoverText : _translation.Get("ui.tasks.options.name"), 
            _nameTextBox.Y,
             _nameTextBox.Text.TrimEnd().Length > 0 ? Game1.textColor : Color.DarkRed);
            _nameTextBox.Draw(b);
            headerButton.draw(b, Color.White, 0.88f, (isHeader || _hoverId == headerButton.myID) ? 1 : 0);

            weekdaysDropDown.visible = renewPeriod == Period.Weekly;
            seasonsDropDown.visible = renewPeriod == Period.Annually;
            daysDropDown.visible = renewPeriod == Period.Monthly || renewPeriod == Period.Annually;
            customRenewTextBoxCC.visible = renewPeriod == Period.Custom;

            if (daysDropDown.visible)
            {
                daysDropDown.bounds.X = (renewPeriod == Period.Annually ? seasonsDropDown.bounds.Right : renewPeriodDropDown.bounds.Right) + 8;
                daysDropDown.RecalculateBounds();
            }
            
            if (ShowAdvancedOptions)
            {
                DrawCheckboxGroup(b, _translation.Get("ui.tasks.options.weather"), weatherCheckboxes, i => 
                    _weatherFlags.HasFlag(i == 0 ? WeatherFlags.Sun : WeatherFlags.Rain));
                DrawCheckboxGroup(b, _translation.Get("ui.tasks.options.weeks"), weekCheckboxes, i => 
                    _weekFlags.HasFlag((WeekFlags)(1 << i)));
                DrawCheckboxGroup(b, _translation.Get("ui.tasks.options.days"), weekdayCheckboxes, i => 
                    _weekdayFlags.HasFlag((WeekdayFlags)(1 << i)));
                DrawCheckboxGroup(b, _translation.Get("ui.tasks.options.seasons"), seasonCheckboxes, i => 
                    _seasonFlags.HasFlag((SeasonFlags)(1 << i)));
            }

            DrawLabel(b, _translation.Get("ui.tasks.options.renew"), renewPeriodDropDown.bounds.Y, Game1.textColor);
            renewPeriodDropDown.Draw(b);
            weekdaysDropDown.Draw(b);
            seasonsDropDown.Draw(b);
            daysDropDown.Draw(b);

            if (customRenewTextBoxCC.visible)
            {
                _customRenewTextBox.Draw(b);

                if (string.IsNullOrEmpty(_customRenewTextBox.Text))
                {
                    Utility.drawTextWithShadow(b, 
                    _translation.Get("ui.tasks.renew.days", new { count = "#" }), 
                    Game1.smallFont, 
                    new(_customRenewTextBox.X + 28, _customRenewTextBox.Y + 12), 
                    Game1.unselectedOptionColor);
                }
            }

            if (_hoverText.Length > 0) 
            {
                drawHoverText(b, _hoverText, Game1.dialogueFont);
            }
        }

        private void DrawCheckboxGroup(SpriteBatch b, string label, IList<ClickableComponent> checkboxes, Func<int, bool> isChecked)
        {
            if (checkboxes == null || checkboxes.Count == 0) return;

            DrawLabel(b, label, checkboxes[0].bounds.Y + 2, Game1.textColor);

            for (int i = 0; i < checkboxes.Count; i++)
            {
                var cb = checkboxes[i];
                b.Draw(Game1.mouseCursors, new Vector2(cb.bounds.X, cb.bounds.Y), 
                    isChecked(i) ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked, 
                    Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);

                Utility.drawTextWithShadow(b, cb.label, Game1.smallFont, new Vector2(cb.bounds.X + 40, cb.bounds.Y + 8), Game1.textColor);
            }
        }

        private void DrawLabel(SpriteBatch b, string name, int yPos, Color color) 
        { 
            Utility.drawTextWithShadow(b, name, Game1.dialogueFont, new Vector2(_sharedContentBounds.X, yPos), color); 
        }

        private static void DrawRightButtonBox(SpriteBatch b, Rectangle boxBounds, Color color)
        {
            Rectangle fillBounds = new(boxBounds.X, boxBounds.Y + InnerBorderToEdge, boxBounds.Width - InnerBorderToEdge, boxBounds.Height - InnerBorderToEdge * 2);

            b.Draw(Game1.menuTexture, fillBounds, new(64, 160, 32, 32), color);
            b.Draw(Game1.menuTexture, new Rectangle(boxBounds.Right - 64, boxBounds.Y, 64, 64), new(192, 0, 64, 64), color);
            b.Draw(Game1.menuTexture, new Rectangle(boxBounds.Right - 64, boxBounds.Bottom - 72, 64, 8), new(192, 128, 64, 8), color);
            b.Draw(Game1.menuTexture, new Rectangle(boxBounds.Right - 64, boxBounds.Bottom - 64, 64, 64), new(192, 192, 64, 64), color);
            b.Draw(Game1.menuTexture, new Rectangle(boxBounds.X, boxBounds.Y, boxBounds.Width - 64, 64), new(128, 0, 12, 64), color);
            b.Draw(Game1.menuTexture, new Rectangle(boxBounds.X, boxBounds.Bottom - 64, boxBounds.Width - 64, 64), new(128, 192, 12, 64), color);
            b.Draw(Game1.menuTexture, new Rectangle(boxBounds.X - InnerBorderToEdge, boxBounds.Y + 12, 32, 24), new(0, 84, 32, 24), color);
            b.Draw(Game1.menuTexture, new Rectangle(fillBounds.X, boxBounds.Y + 20, 12, 12), new(32, 92, 12, 12), color);
            b.Draw(Game1.menuTexture, new Rectangle(boxBounds.X - InnerBorderToEdge, boxBounds.Bottom - 36, 32, 24), new(0, 84, 32, 24), color);
            b.Draw(Game1.menuTexture, new Rectangle(fillBounds.X, boxBounds.Bottom - 32, 12, 12), new(32, 88, 12, 12), color);
        }

        private void ApplyChangesAndExit(bool playSound = true) 
        { 
            ApplyChanges(); 

            if (playSound) 
            {
                Game1.playSound("bigSelect"); 
            }

                ExitAllChildMenus(false); 
        }

        private void ExitAllChildMenus(bool playSound = true) 
        { 
            if (playSound) 
            {
                Game1.playSound("bigDeSelect");
            } 

                for (IClickableMenu parent = GetParentMenu(); parent != null; parent = parent.GetParentMenu())
            { 
                parent.GetChildMenu().exitThisMenuNoSound(); 
            }
        }

        private void OnExit() 
        { 
            Game1.keyboardDispatcher.Subscriber = null; 

            if (Game1.options.SnappyMenus) 
            {
                Game1.activeClickableMenu?.snapToDefaultClickableComponent(); 
            }
        }
    }
}
