using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;
using DeluxeJournal.Task;

using static DeluxeJournal.Task.TaskParameterAttribute;

namespace DeluxeJournal.Menus.Components
{
    /// <summary>A <see cref="SideScrollingTextBox"/> that parses input for a <see cref="TaskParameter"/>.</summary>
    public class TaskParameterTextBox : SideScrollingTextBox, ITaskParameterComponent
    {
        public ClickableComponent ClickableComponent { get; }

        public TaskParameter Parameter { get; set; }

        public TaskParser Parser { get; }

        public string Label { get; set; } = string.Empty;

        public TaskParameterTextBox(TaskParameter parameter, Task.TaskFactory factory, ClickableComponent component, Texture2D? textBoxTexture, Texture2D? caretTexture, SpriteFont font, Color textColor, ITranslationHelper translation)
            : base(textBoxTexture, caretTexture, font, textColor)
        {
            ClickableComponent = component;
            Parameter = parameter;
            Parser = new TaskParser(translation, new()
            {
                EnableFuzzySearch = true,
                IgnoreItems = !parameter.Attribute.Tag.Equals(TaskParameterTag.ItemList),
                IgnoreForageItems = !parameter.Attribute.Tag.Equals(TaskParameterTag.ForageItemList),
                IgnoreNpcs = !parameter.Attribute.Tag.Equals(TaskParameterTag.NpcName),
                IgnoreBuildings = !parameter.Attribute.Tag.Equals(TaskParameterTag.Building),
                IgnoreFarmAnimals = !parameter.Attribute.Tag.Equals(TaskParameterTag.FarmAnimalList),
                IgnorePets = !parameter.Attribute.Tag.Equals(TaskParameterTag.PetName),
                IgnoreLocations = !parameter.Attribute.Tag.Equals(TaskParameterTag.Location),
                IgnoreFarmLocations = !parameter.Attribute.Tag.Equals(TaskParameterTag.FarmLocation),
                IgnoreForageLocations = !parameter.Attribute.Tag.Equals(TaskParameterTag.ForageLocation),
                IgnoreAnimalLocations = !parameter.Attribute.Tag.Equals(TaskParameterTag.AnimalLocation),
                IgnoreMachineLocations = !parameter.Attribute.Tag.Equals(TaskParameterTag.MachineLocation),
                IgnoreMachines = !parameter.Attribute.Tag.Equals(TaskParameterTag.Machine),
                IgnoreSpecialOrders = !parameter.Attribute.Tag.Equals(TaskParameterTag.SpecialOrderType),
                IgnorePassiveFestivals = !parameter.Attribute.Tag.Equals(TaskParameterTag.PassiveFestivalId),
                IgnoreActiveFestivals = !parameter.Attribute.Tag.Equals(TaskParameterTag.ActiveFestivalId),
                IgnoreShops = !parameter.Attribute.Tag.Equals(TaskParameterTag.Shop),
                SetItemCategoryObject = parameter.Attribute.Constraints.HasFlag(Constraint.SObject),
                SetItemCategoryBigCraftable = parameter.Attribute.HasAnyConstraint(Constraint.SObject ^ Constraint.Craftable),
                SetItemCategoryCraftable = parameter.Attribute.Constraints.HasFlag(Constraint.Craftable),
                SetItemCategoryTool = parameter.Attribute.Constraints.HasFlag(Constraint.Upgradable)
            })
            {
                Factory = factory
            };

            Parser.ApplyParameterValue(parameter);
            FillWithParsedText(true);
        }

        /// <summary>Fill the text field with text and parse it.</summary>
        /// <param name="text">Text to parse.</param>
        /// <inheritdoc cref="FillWithParsedText(bool)"/>
        public bool FillWithParsedText(string text, bool caseSensitive = false)
        {
            Text = text;
            return FillWithParsedText(caseSensitive);
        }

        /// <summary>Fill the text field with the display name provided by the parser.</summary>
        /// <param name="caseSensitive">Include letter case when checking if the text was changed.</param>
        /// <returns><c>true</c> if the text was changed and <c>false</c> if it remained the same.</returns>
        public bool FillWithParsedText(bool caseSensitive = false)
        {
            string previous = Text;

            Text = Parameter.Attribute.Tag switch
            {
                TaskParameterTag.ItemList => Parser.ProxyItemDisplayName,
                TaskParameterTag.ForageItemList => Parser.ProxyItemDisplayName,
                TaskParameterTag.NpcName => Parser.NpcDisplayName,
                TaskParameterTag.Building => Parser.BuildingDisplayName,
                TaskParameterTag.FarmAnimalList => Parser.FarmAnimalDisplayName,
                TaskParameterTag.PetName => Parser.PetDisplayName,
                TaskParameterTag.Count => Parser.Count.ToString(),
                TaskParameterTag.Location => Parser.LocationDisplayName,
                TaskParameterTag.FarmLocation => Parser.FarmLocationDisplayName,
                TaskParameterTag.ForageLocation => Parser.ForageLocationDisplayName,
                TaskParameterTag.AnimalLocation => Parser.AnimalLocationDisplayName,
                TaskParameterTag.MachineLocation => Parser.MachineLocationDisplayName,
                TaskParameterTag.Shop => Parser.ShopDisplayName,
                TaskParameterTag.Machine => Parser.MachineDisplayName,
                TaskParameterTag.SpecialOrderType => Parser.SpecialOrderType,
                TaskParameterTag.PassiveFestivalId => Parser.PassiveFestivalDisplayName,
                TaskParameterTag.ActiveFestivalId => Parser.ActiveFestivalDisplayName,

                _ => string.Empty
            };

            if (!caseSensitive)
            {
                return previous.ToLower() != Text.ToLower();
            }

            return previous != Text;
        }

        public IEnumerable<ClickableComponent> GetClickableComponents()
        {
            yield return ClickableComponent;
        }

        public void RecalculateBounds()
        {
            X = ClickableComponent.bounds.X;
            Y = ClickableComponent.bounds.Y;
            Width = ClickableComponent.bounds.Width;
            Height = ClickableComponent.bounds.Height;
        }

        public void TryHover(int x, int y)
        {
        }

        public void ReceiveLeftClick(int x, int y, bool playSound = true)
        {
            SelectMe();
            ForceUpdate();
        }

        public override void RecieveTextInput(char inputChar)
        {
            base.RecieveTextInput(inputChar);
            UpdateParameter();
        }

        public override void RecieveTextInput(string text)
        {
            base.RecieveTextInput(text);
            UpdateParameter();
        }

        public override void RecieveCommandInput(char command)
        {
            base.RecieveCommandInput(command);

            switch (command)
            {
                case '\b':
                    UpdateParameter();
                    break;
            }
        }

        public void Draw(SpriteBatch b)
        {
            base.Draw(b);
        }

        private void UpdateParameter()
        {
            if (string.IsNullOrEmpty(Text))
            {
                ClearParameterValue();
            }
            else if (!Parser.Parse(Text, TaskParser.ParseMode.UpdateFactory))
                {
                    Parameter.Value = null;
            }
        }

        private void ClearParameterValue()
        {
            Parameter.Value = null;
            Parser.ApplyParameterValue(Parameter);
        }
    }
}
