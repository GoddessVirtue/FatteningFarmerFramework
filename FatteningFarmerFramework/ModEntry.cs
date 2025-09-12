using System;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace FatteningFarmerFramework;

public sealed class ModConfig {
    public string WeightUnits { get; set; }
    public int StartingWeight { get; set; }
    public int MinimumWeight { get; set; }
    public int UnmoddedTextureWeight { get; set; }
    public double WeightGainSpeed { get; set; }
    public double FestivalWeightGain  { get; set; }
    public double ExerciseEffect { get; set; }
    public double BasalMetabolicRate { get; set; }
    public double NewtonsSecondLaw { get; set; }
    public bool WeightNotifications { get; set; }
    public bool StrictClothingCompatibility { get; set; }
    public int Nudity { get; set; }

    public ModConfig() {
        this.WeightUnits = "pounds";
        this.StartingWeight = 120;
        this.MinimumWeight = 120;
        this.UnmoddedTextureWeight = 120;
        this.WeightGainSpeed = 1;
        this.FestivalWeightGain = 3;
        this.ExerciseEffect = 1;
        this.BasalMetabolicRate = 0.5;
        this.NewtonsSecondLaw = 0.33;
        this.WeightNotifications = true;
        this.StrictClothingCompatibility = false;
        this.Nudity = 1;
    }
}

public enum AppearanceType {
    Body,
    Shirt,
    Pants
}

[UsedImplicitly]
public sealed class IndividualItemSize {
    public readonly string ContentPackID;
    public readonly string Texture;
    public readonly string Fashion;
    
    public IndividualItemSize(string contentPackID, string texture, string fashion) {
        this.ContentPackID = contentPackID;
        this.Texture = texture;
        this.Fashion = fashion;
    }
}

public class Size {
    public readonly int Weight;
    public readonly string ContentPackID;
    public readonly string Texture;
    public readonly Dictionary<int, IndividualItemSize> IndividualItemTextures;

    public Size(int weight, string texture) {
        this.Weight = weight;
        this.ContentPackID = "";
        this.Texture = texture;
        this.IndividualItemTextures = new Dictionary<int, IndividualItemSize>();
    }
}

public class BodySize : Size {
    public readonly bool Fashion; //If Fashion is true, then texture is a Fashion Sense asset, not a Stardew asset
    
    public BodySize(int weight, string texture, bool fashion): base(weight, texture) {
        this.Fashion = fashion;
    }
}

public sealed class ModData {
    private readonly ModConfig _config;
    private double _weight;
    public Size BodySize;
    public Size ShirtSize;
    public Size PantsSize;
    public double Weight {
        get => Math.Round(_weight, 2);
        set {
            _weight = Math.Round(value, 2);
            if (_weight < this._config.MinimumWeight) {
                _weight = this._config.MinimumWeight;
            }
        }
    }

    public ModData() : this(new ModConfig()) {}
    
    public ModData(ModConfig config) {
        this._config = config;
        this._weight = config.StartingWeight;
        this.BodySize = new Size(this._config.UnmoddedTextureWeight, "Characters/Farmer/farmer_girl_base");
        this.ShirtSize = new Size(this._config.UnmoddedTextureWeight,"Characters/Farmer/shirts");
        this.PantsSize = new Size(this._config.UnmoddedTextureWeight,"Characters/Farmer/pants");
    }
}

internal sealed class ModEntry : Mod {
    private ModConfig _config = new ModConfig();
    private BodySize[] _femaleBodySizes = [];
    private BodySize[] _maleBodySizes = [];
    private Size[] _shirtSizes = [];
    private Size[] _pantsSizes = [];
    private string _currentShirt = "";
    private string _currentPants = "";
    private Event? _atFestival = null;
    
    
    public override void Entry(IModHelper helper) {
        this._config = this.Helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += this.OnAssetInvalidated;
        helper.Events.Player.Warped += this.OnWarped;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
        CreateConfigMenu();
        this._femaleBodySizes = Game1.content.Load<Dictionary<int, BodySize>>("Mods/FatteningFarmerFramework/Data/FemaleBodySizes").Values.ToArray();
        this._femaleBodySizes = this._femaleBodySizes.Append(new BodySize(this._config.UnmoddedTextureWeight, "Characters/Farmer/farmer_girl_base", false)).ToArray();
        this._maleBodySizes = Game1.content.Load<Dictionary<int, BodySize>>("Mods/FatteningFarmerFramework/Data/MaleBodySizes").Values.ToArray();
        this._maleBodySizes = this._maleBodySizes.Append(new BodySize(this._config.UnmoddedTextureWeight, "Characters/Farmer/farmer_base", false)).ToArray();
        this._shirtSizes = Game1.content.Load<Dictionary<int, Size>>("Mods/FatteningFarmerFramework/Data/ShirtSizes").Values.ToArray();
        this._shirtSizes = this._shirtSizes.Append(new Size(this._config.UnmoddedTextureWeight, "Characters/Farmer/shirts")).ToArray();
        this._pantsSizes = Game1.content.Load<Dictionary<int, Size>>("Mods/FatteningFarmerFramework/Data/PantsSizes").Values.ToArray();
        this._pantsSizes = this._pantsSizes.Append(new Size(this._config.UnmoddedTextureWeight, "Characters/Farmer/pants")).ToArray();

        foreach (BodySize size in this._femaleBodySizes) {
            this.Monitor.Log($"Loaded female body size: {size.Weight}", LogLevel.Debug);
        }
    }

    private void CreateConfigMenu() {
        // get Generic Mod Config Menu's API (if it's installed)
        IGenericModConfigMenuApi? configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null) {
            return;
        }

        // register mod
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => this._config = new ModConfig(),
            save: () => this.Helper.WriteConfig(this._config)
        );

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Starting weight",
            tooltip: () => "The weight of your farmer when you first load this mod or a new save.",
            getValue: () => this._config.StartingWeight,
            setValue: (value) => this._config.StartingWeight = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Minimum weight",
            tooltip: () => "Your weight cannot fall below this value. Maybe your farmer has a secret snack stash for emergencies?",
            getValue: () => this._config.MinimumWeight,
            setValue: (value) => this._config.MinimumWeight = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Unmodded texture weight",
            tooltip: () => "What character weight should the game assume unmodded textures (and non-FFF-compatible mod textures) are equivalent to? This may be useful if you're using different default Farmer textures or a different weight measuring system.",
            getValue: () => this._config.UnmoddedTextureWeight,
            setValue: (value) => this._config.UnmoddedTextureWeight = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Weight gain speed",
            tooltip: () => "How much does food fatten you up? Default value 1 means 100 energy puts on 1 pound. (If you only eat when you need to)",
            getValue: () => (float) this._config.WeightGainSpeed,
            setValue: (value) => this._config.WeightGainSpeed = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Festival weight gain",
            tooltip: () => "If a festival has a buffet, how much weight will you gain from filling yourself up at it?",
            getValue: () => (float) this._config.FestivalWeightGain,
            setValue: (value) => this._config.FestivalWeightGain = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Exercise effect",
            tooltip: () => "How much does a day of exercise slim you down when you're at your minimum weight? Default value 1 means an empty energy bar burns 1 pound.",
            getValue: () => (float) this._config.ExerciseEffect,
            setValue: (value) => this._config.ExerciseEffect = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Basal metabolic rate",
            tooltip: () => "How many weight units of fat does your body burn in a day when you're not doing anything?",
            getValue: () => (float) this._config.BasalMetabolicRate,
            setValue: (value) => this._config.BasalMetabolicRate = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Newton's second law",
            tooltip: () => "Bigger things are harder to move. How much harder? Default value 0.33 means your weight loss speeds up by a third of the base for every 100 weight units over your minimum.",
            getValue: () => (float) this._config.NewtonsSecondLaw,
            setValue: (value) => this._config.NewtonsSecondLaw = value
        );
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Weight notifications",
            tooltip: () => "Do you want to know your weight each morning and whenever you gain a pound? (Note: Weight in the morning may round up, but you will still get a notification when your weight reaches the whole number.)",
            getValue: () => this._config.WeightNotifications,
            setValue: (value) => this._config.WeightNotifications = value
        );
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Strict clothing compatibility",
            tooltip: () => "Textures may look odd if your body and your clothing are different sizes. When this setting is enabled, your textures will only go up a size when the weight for the body, shirt, and pants match.",
            getValue: () => this._config.StrictClothingCompatibility,
            setValue: (value) => this._config.StrictClothingCompatibility = value
        );
        configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Nudity",
            tooltip: () => "Where we're going, we don't need boxer shorts! 0: The player will wear red boxers and an undershirt (or whatever underwear your other mods add) at all sizes. 1: Underwear textures will be removed at modded body sizes. 2: The player will never wear underwear textures.",
            getValue: () => this._config.Nudity,
            setValue: (value) => this._config.Nudity = value,
            min: 0,
            max: 2
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
        ModData? playerStats = this.Helper.Data.ReadSaveData<ModData>("player-stats");
        if (playerStats == null) {
            playerStats = new ModData(this._config);
            this.Helper.Data.WriteSaveData<ModData>("player-stats", playerStats);
        }

        this._currentShirt = Game1.player.shirtItem.Value != null ? Game1.player.shirtItem.Value.Name : "";
        this._currentPants = Game1.player.pantsItem.Value != null ? Game1.player.pantsItem.Value.Name : "";
        
        this.CalculateSize(Game1.player);
    }
    
    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e) {
        if (!e.IsLocalPlayer) {return;}
        
        if (e.Player.isEating) {
            OnEat(e.Player);
        }

        string shirt = e.Player.shirtItem.Value != null ? e.Player.shirtItem.Value.Name : "";
        string pants =  e.Player.pantsItem.Value != null ? e.Player.pantsItem.Value.Name : "";
        if (shirt != this._currentShirt || pants != this._currentPants) {
            this.Monitor.Log("Recalculating size because clothes changed", LogLevel.Debug);
            //The player changed their clothes. Let's recalculate their size in case the new clothes affect the size calculations
            this.CalculateSize(e.Player);
            this._currentShirt = shirt;
            this._currentPants = pants;
        }
    }

    private void OnEat(Farmer player) {
        Item itemToEat = player.itemToEat;
        if (itemToEat is not StardewValley.Object) {
            return;
        }
        StardewValley.Object food = (StardewValley.Object) itemToEat;
        
        //Gain weight from eating food, and more if overeating
        double weightToGain = food.Edibility / 40.00 * this._config.WeightGainSpeed;
        double remainingEnergy = player.MaxStamina - player.Stamina;
        double excessEdibility = food.Edibility - remainingEnergy * 0.4 ; //Edibility left over after energy is filled up
        if (excessEdibility > 0) {
            weightToGain += excessEdibility / 40.00 * this._config.WeightGainSpeed;
        }
        weightToGain = Math.Round(weightToGain, 2);
        
        ModData playerStats = this.Helper.Data.ReadSaveData<ModData>("player-stats")??new ModData(this._config);
        playerStats.Weight += weightToGain;
        this.Helper.Data.WriteSaveData<ModData>("player-stats",  playerStats);
        this.Monitor.Log($"{player.Name} ate {food.Name}, gained {weightToGain} {this._config.WeightUnits}, and now weighs {playerStats.Weight} {this._config.WeightUnits}.", LogLevel.Trace);
        
        if (Math.Floor(playerStats.Weight - weightToGain) < Math.Floor(playerStats.Weight)) {
            if (this._config.WeightNotifications) {
                Game1.addHUDMessage(new HUDMessage($"You now weigh {Math.Round(playerStats.Weight, 0)} {this._config.WeightUnits}."));
            }
            this.CalculateSize(player);
        }
        
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e) {
        Farmer player = Game1.player;
        ModData playerStats = this.Helper.Data.ReadSaveData<ModData>("player-stats")??new ModData(this._config);
        double weightEnergyFactor = 1 + this._config.NewtonsSecondLaw * (playerStats.Weight - this._config.MinimumWeight) / 100;
        
        double energyDepletion = (player.MaxStamina - player.Stamina) / player.MaxStamina;
        double exerciseWeightLoss = Math.Round(energyDepletion * this._config.ExerciseEffect * weightEnergyFactor, 2);
        double basalWeightLoss = Math.Round(this._config.BasalMetabolicRate * weightEnergyFactor, 2);
        double weightToLose = exerciseWeightLoss + basalWeightLoss;
        playerStats.Weight -= weightToLose;
        this.Helper.Data.WriteSaveData<ModData>("player-stats",  playerStats);
        this.CalculateSize(Game1.player);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e) {
        if (this._config.WeightNotifications) {
            ModData playerStats = this.Helper.Data.ReadSaveData<ModData>("player-stats")??new ModData(this._config);
            Game1.addHUDMessage(new HUDMessage($"Today's morning weigh-in: {Math.Round(playerStats.Weight, 1)} {this._config.WeightUnits}."));
        }
    }

    private void CalculateSize(Farmer player) {
        ModData playerStats = this.Helper.Data.ReadSaveData<ModData>("player-stats")??new ModData(this._config);
        int weight = (int) Math.Floor(playerStats.Weight);
        BodySize[] bodySizes;
        if (player.Gender == Gender.Male) {
            bodySizes = _maleBodySizes;
        }
        else if (player.Gender == Gender.Female) {
            bodySizes = _femaleBodySizes;
        }
        else {
            this.Monitor.Log($"Could not resolve body sizes for player gender {player.Gender}", LogLevel.Debug);
            return;
        }
        BodySize bodySize = new BodySize(0, "", false);
        BodySize smallestBody = bodySizes[0];
        foreach (BodySize size in bodySizes) {
            if (this._config.StrictClothingCompatibility) { //We won't be using any body sizes that don't have a matching shirt and pants size
                bool underclothesEnabled = this._config.Nudity == 0 || (size.Weight == this._config.UnmoddedTextureWeight && this._config.Nudity == 1);
                if (player.shirtItem.Value != null || underclothesEnabled){
                    bool shirtCompatible = false;
                    foreach (Size sSize in _shirtSizes) {
                        if (sSize.Weight == size.Weight) {
                            shirtCompatible = true;
                        }
                    }

                    if (!shirtCompatible) {
                        this.Monitor.Log($"Discarded body size {size.Weight} because there was no matching shirt size.",
                            LogLevel.Trace);
                        continue;
                    }
                }
                if (player.pantsItem.Value != null || underclothesEnabled){
                    bool pantsCompatible = false;
                    foreach (Size pSize in _pantsSizes) {
                        if (pSize.Weight == size.Weight) {
                            pantsCompatible = true;
                        }
                    }

                    if (!pantsCompatible) {
                        this.Monitor.Log($"Discarded body size {size.Weight} because there was no matching pants size.",
                            LogLevel.Trace);
                        continue;
                    }
                }
            }
            if (size.Weight > bodySize.Weight && size.Weight <= weight) {
                bodySize = size; //Find the biggest body size that is below player weight
            }
            if (size.Weight < smallestBody.Weight) {
                smallestBody = size; //Find the smallest body size, just in case
            }
        }
        if (bodySize.Weight < smallestBody.Weight) {bodySize = smallestBody;} //If no small enough body has been found, just use the smallest
        playerStats.BodySize = bodySize;
        this.Helper.Data.WriteSaveData<ModData>("player-stats",  playerStats);

        if (this._config.StrictClothingCompatibility) { //We already know shirt and pants size will be equal to body size
            foreach (Size size in _shirtSizes) {
                if (size.Weight == bodySize.Weight) {
                    playerStats.ShirtSize = size;
                    break;
                }
            }
            foreach (Size size in _pantsSizes) {
                if (size.Weight == bodySize.Weight) {
                    playerStats.PantsSize = size;
                    break;
                }
            }
            this.Helper.Data.WriteSaveData<ModData>("player-stats",  playerStats);
        }
        else {
            int shirtSpriteIndex = Game1.player.shirtItem.Value != null ? Game1.player.shirtItem.Value.indexInTileSheet.Value : 41;
            Size shirtSize = new Size(0, "");
            Size smallestShirt = _shirtSizes[0];
            foreach (Size size in _shirtSizes) {
                if (string.IsNullOrEmpty(size.Texture)) {
                    if (!size.IndividualItemTextures.ContainsKey(shirtSpriteIndex) || (string.IsNullOrEmpty(size.IndividualItemTextures[shirtSpriteIndex].Texture) && string.IsNullOrEmpty(size.IndividualItemTextures[shirtSpriteIndex].Fashion))) {
                        this.Monitor.Log(
                            $"Discarded shirt size {size.Weight} because none of the textures matched the equipped shirt at sprite index {shirtSpriteIndex}.",
                            LogLevel.Trace);
                    }
                }

                if (size.Weight > shirtSize.Weight && size.Weight <= weight) {
                    shirtSize = size; //Find the biggest shirt size that is below player weight
                }

                if (size.Weight < smallestShirt.Weight) {
                    smallestShirt = size; //Find the smallest shirt size, just in case
                }
            }

            if (shirtSize.Weight < smallestShirt.Weight) {
                shirtSize = smallestShirt;
            } //If no small enough shirt has been found, just use the smallest

            playerStats.ShirtSize = shirtSize;
            this.Helper.Data.WriteSaveData<ModData>("player-stats", playerStats);

            int pantsSpriteIndex = Game1.player.pantsItem.Value != null ? Game1.player.pantsItem.Value.indexInTileSheet.Value: 14;
            Size pantsSize = new Size(0, "");
            Size smallestPants = _pantsSizes[0];
            foreach (Size size in _pantsSizes) {
                if (string.IsNullOrEmpty(size.Texture)) {
                    if (!size.IndividualItemTextures.ContainsKey(pantsSpriteIndex) || (string.IsNullOrEmpty(size.IndividualItemTextures[pantsSpriteIndex].Texture) && string.IsNullOrEmpty(size.IndividualItemTextures[pantsSpriteIndex].Texture))) {
                        this.Monitor.Log(
                            $"Discarded pants size {size.Weight} because none of the textures matched the equipped pants at sprite index {pantsSpriteIndex}.",
                            LogLevel.Trace);
                    }
                }

                if (size.Weight > pantsSize.Weight && size.Weight <= weight) {
                    pantsSize = size; //Find the biggest pants size that is below player weight
                }

                if (size.Weight < smallestPants.Weight) {
                    smallestPants = size; //Find the smallest pants size, just in case
                }
            }

            if (pantsSize.Weight < smallestPants.Weight) {
                pantsSize = smallestPants;
            } //If no small enough pants have been found, just use the smallest

            playerStats.PantsSize = pantsSize;
            this.Helper.Data.WriteSaveData<ModData>("player-stats", playerStats);
        }

        //The player's weight has changed and their sizes have updated. Time to reload the player textures
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/farmer_base");
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/farmer_base_bald");
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/farmer_girl_base");
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/farmer_girl_base_bald");
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/shirts");
        this.Helper.GameContent.InvalidateCache("Data/Shirts");
        this.Helper.GameContent.InvalidateCache("Characters/Farmer/pants");
        if (!string.IsNullOrEmpty(playerStats.BodySize.Texture)) {
            this.Helper.GameContent.InvalidateCache(this.Helper.ModContent.GetInternalAssetName(playerStats.BodySize.Texture));
        }
        if (!string.IsNullOrEmpty(playerStats.ShirtSize.Texture)) {
            this.Helper.GameContent.InvalidateCache(this.Helper.ModContent.GetInternalAssetName(playerStats.ShirtSize.Texture));
        }
        if (!string.IsNullOrEmpty(playerStats.PantsSize.Texture)) {
            this.Helper.GameContent.InvalidateCache(this.Helper.ModContent.GetInternalAssetName(playerStats.PantsSize.Texture));
        
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
        if (e.Name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/FemaleBodySizes")) {
            e.LoadFromModFile<Dictionary<int, Size>>("assets/data/female_body_sizes.json", AssetLoadPriority.Low);
        }
        if (e.Name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/MaleBodySizes")) {
            e.LoadFromModFile<Dictionary<int, Size>>("assets/data/male_body_sizes.json", AssetLoadPriority.Low);
        }
        if (e.Name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/ShirtSizes")) {
            e.LoadFromModFile<Dictionary<int, Size>>("assets/data/shirt_sizes.json", AssetLoadPriority.Low);
        }
        if (e.Name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/PantsSizes")) {
            e.LoadFromModFile<Dictionary<int, Size>>("assets/data/pants_sizes.json", AssetLoadPriority.Low);
        }
        
        if (!Context.IsWorldReady) {
            return; //The save hasn't loaded yet, so we won't be able to do anything involving player weight
        }
        
        ModData playerStats = this.Helper.Data.ReadSaveData<ModData>("player-stats")??new ModData(this._config);
        if (e.Name.IsEquivalentTo("Characters/Farmer/farmer_base") || e.Name.IsEquivalentTo("Characters/Farmer/farmer_base_bald") 
                || e.Name.IsEquivalentTo("Characters/Farmer/farmer_girl_base") || e.Name.IsEquivalentTo("Characters/Farmer/farmer_girl_base_bald")) {
            //The game has requested a body asset
            Size bodySize = playerStats.BodySize;
            if (bodySize.Weight == this._config.UnmoddedTextureWeight) {
                return; //The player's body size matches the vanilla textures, so the FFF should do nothing about body textures
            }

            this.Monitor.Log($"Loading body asset file: {bodySize.Texture}", LogLevel.Trace);
            PatchSpritesheet(e, AppearanceType.Body, [(0, bodySize.Texture)]);
        }

        if (e.Name.IsEquivalentTo("Characters/Farmer/shirts")) {
            //The game has requested a shirt asset
            Size shirtSize = playerStats.ShirtSize;
            Size bodySize = playerStats.BodySize;
            List<(int, string)> patches = [];
            
            if (this._config.Nudity == 2 || (bodySize.Weight != this._config.UnmoddedTextureWeight && this._config.Nudity == 1)) {
                //Remove the undershirt from the shirts texture
                patches.Add((41, "assets/utilities/8x32_transparency.png")); 
                patches.Add((41, "assets/utilities/skin_tones.png"));
                PatchSpritesheet(e, AppearanceType.Shirt, patches.ToArray());
                patches = [];
            }
            
            if (shirtSize.Weight == this._config.UnmoddedTextureWeight) {
                return; //The player's shirt size matches the vanilla textures, so the FFF should stop after removing the undershirt
            }

            int shirtSpriteIndex = Game1.player.shirtItem.Value != null ? Game1.player.shirtItem.Value.indexInTileSheet.Value : 41;
            if (!shirtSize.IndividualItemTextures.ContainsKey(shirtSpriteIndex) || string.IsNullOrEmpty(shirtSize.IndividualItemTextures[shirtSpriteIndex].Texture)) {
                this.Monitor.Log($"Loading shirts spritesheet: {shirtSize.Texture}", LogLevel.Trace);
                patches.Add((0, shirtSize.Texture));
            }
            else if (string.IsNullOrEmpty(shirtSize.IndividualItemTextures[shirtSpriteIndex].Fashion)) {
                this.Monitor.Log($"Loading shirt sprite: {shirtSize.IndividualItemTextures[shirtSpriteIndex].Texture}", LogLevel.Trace);
                patches.Add((shirtSpriteIndex, shirtSize.IndividualItemTextures[shirtSpriteIndex].Texture));
            }
            else { //A content pack has registered a Fashion Sense appearance to use
                string fashion = shirtSize.IndividualItemTextures[shirtSpriteIndex].Fashion;
                string contentPackID = shirtSize.IndividualItemTextures[shirtSpriteIndex].ContentPackID;
                IFashionSenseIApi? fashionSense = this.Helper.ModRegistry.GetApi<IFashionSenseIApi>("PeacefulEnd.FashionSense");
                if (fashionSense == null) {
                    this.Monitor.Log($"Fashion Sense is not installed but {contentPackID} tried to register a Fashion Sense appearance. " +
                                     "Please install Fashion Sense at https://www.nexusmods.com/stardewvalley/mods/9969.", LogLevel.Error);
                    return;
                }
                KeyValuePair<bool, string> response = fashionSense.SetAppearance(IFashionSenseIApi.Type.Shirt, contentPackID, fashion, this.ModManifest);
                if (!response.Key) {
                    this.Monitor.Log($"Fashion Sense API reports problem: {response.Value}", LogLevel.Error);
                }
                else {
                    this.Monitor.Log($"Fashion Sense API reports success: {response.Value}", LogLevel.Debug);
                }
            }
            PatchSpritesheet(e, AppearanceType.Shirt, patches.ToArray());
        }

        if (e.Name.IsEquivalentTo("Characters/Farmer/Pants")) {
            //The game has requested a pants asset
            Size pantsSize = playerStats.PantsSize;
            Size bodySize = playerStats.BodySize;
            List<(int, string)> patches = [];
            
            if (this._config.Nudity == 2 || (bodySize.Weight != this._config.UnmoddedTextureWeight && this._config.Nudity == 1)) {
                //Remove the boxer shorts from the pants texture
                patches.Add((28, "assets/utilities/96x688_transparency.png"));
                patches.Add((29, "assets/utilities/96x688_transparency.png"));
                PatchSpritesheet(e, AppearanceType.Pants, patches.ToArray());
                patches = [];
            }
            
            if (pantsSize.Weight == this._config.UnmoddedTextureWeight) {
                return; //The player's pants size matches the vanilla textures, so the FFF should stop after removing the boxers
            }
            
            int pantsSpriteIndex = Game1.player.pantsItem.Value != null ? Game1.player.pantsItem.Value.indexInTileSheet.Value: 14;
            if (!pantsSize.IndividualItemTextures.ContainsKey(pantsSpriteIndex) || string.IsNullOrEmpty(pantsSize.IndividualItemTextures[pantsSpriteIndex].Texture)) {
                this.Monitor.Log($"Loading pants spritesheet: {pantsSize.Texture}", LogLevel.Trace);
                patches.Add((0, pantsSize.Texture));
            }
            else if (string.IsNullOrEmpty(pantsSize.IndividualItemTextures[pantsSpriteIndex].Fashion)) {
                this.Monitor.Log($"Loading pants sprite: {pantsSize.IndividualItemTextures[pantsSpriteIndex].Texture}", LogLevel.Trace);
                patches.Add((pantsSpriteIndex, pantsSize.IndividualItemTextures[pantsSpriteIndex].Texture));
            }
            else { //A content pack has registered a Fashion Sense appearance to use
                string fashion = pantsSize.IndividualItemTextures[pantsSpriteIndex].Fashion;
                string contentPackID = pantsSize.IndividualItemTextures[pantsSpriteIndex].ContentPackID;
                IFashionSenseIApi? fashionSense = this.Helper.ModRegistry.GetApi<IFashionSenseIApi>("PeacefulEnd.FashionSense");
                if (fashionSense == null) {
                    this.Monitor.Log($"Fashion Sense is not installed but {contentPackID} tried to register a Fashion Sense appearance. " +
                                     "Please install Fashion Sense at https://www.nexusmods.com/stardewvalley/mods/9969.", LogLevel.Error);
                    return;
                }
                KeyValuePair<bool, string> response = fashionSense.SetAppearance(IFashionSenseIApi.Type.Shirt, contentPackID, fashion, this.ModManifest);
                if (!response.Key) {
                    this.Monitor.Log($"Fashion Sense API reports problem: {response.Value}", LogLevel.Error);
                }
            }
            PatchSpritesheet(e, AppearanceType.Pants, patches.ToArray());
        }
    }

    private void PatchSpritesheet(AssetRequestedEventArgs e, AppearanceType type, (int targetIndex, string sourceImage)[] patches) {
        int spriteWidth, spriteHeight, spritesPerRow;
        if (type == AppearanceType.Shirt) {
            spriteWidth = 8;
            spriteHeight = 8;
            spritesPerRow = 16;
        }
        else if (type == AppearanceType.Pants) {
            spriteWidth = 96;
            spriteHeight = 688;
            spritesPerRow = 20;
        }
        else if (type == AppearanceType.Body) {
            spriteWidth = 16;
            spriteHeight = 32;
            spritesPerRow = 6;
        }
        else {
            this.Monitor.Log($"Tried to patch spritesheet {e.Name} but the appearance type {type} was unsupported",  LogLevel.Error);
            return;
        }

        e.Edit(asset => { 
            foreach ((int targetIndex, string sourceImage) in patches) {
                try {
                    IAssetDataForImage editor = asset.AsImage();
                    Texture2D sourceTexture;
                    if (sourceImage.StartsWith("assets/") || sourceImage.EndsWith(".png")) { //If it's an image file from the FFF, use ModContent
                        sourceTexture = this.Helper.ModContent.Load<Texture2D>(sourceImage);
                    }
                    else { //If it's an image asset loaded by a content pack, use GameContent
                        sourceTexture = this.Helper.GameContent.Load<Texture2D>(sourceImage);
                    }
                    int xOffset = targetIndex % spritesPerRow * spriteWidth;
                    int yOffset = targetIndex / spritesPerRow * spriteHeight;
                    Rectangle destinationArea = new Rectangle(xOffset, yOffset, sourceTexture.Width, sourceTexture.Height);
                    
                    editor.ExtendImage(minWidth: sourceTexture.Width + xOffset, minHeight: sourceTexture.Height + yOffset);
                    editor.PatchImage(source: sourceTexture, patchMode: PatchMode.Replace, targetArea: destinationArea);
                }
                catch (Exception ex) {
                    this.Monitor.Log($"Tried to patch spritesheet {e.Name} with {sourceImage} starting at sprite {targetIndex} but there was an error:\n\t{ex.Message}", LogLevel.Error);
                }
            }
        });
    }

    private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
        foreach (IAssetName name in e.Names) {
            if (name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/FemaleBodySizes")) {
                this._femaleBodySizes = Game1.content
                    .Load<Dictionary<int, BodySize>>("Mods/FatteningFarmerFramework/Data/FemaleBodySizes").Values.ToArray();
                this._femaleBodySizes = this._femaleBodySizes
                    .Append(new BodySize(this._config.UnmoddedTextureWeight, "Characters/Farmer/farmer_girl_base", false)).ToArray();
            }

            if (name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/MaleBodySizes")) {
                this._maleBodySizes = Game1.content
                    .Load<Dictionary<int, BodySize>>("Mods/FatteningFarmerFramework/Data/MaleBodySizes").Values.ToArray();
                this._maleBodySizes = this._maleBodySizes
                    .Append(new BodySize(this._config.UnmoddedTextureWeight, "Characters/Farmer/farmer_base", false)).ToArray();
            }

            if (name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/ShirtSizes")) {
                this._shirtSizes = Game1.content
                    .Load<Dictionary<int, Size>>("Mods/FatteningFarmerFramework/Data/ShirtSizes").Values.ToArray();
                this._shirtSizes = this._shirtSizes
                    .Append(new Size(this._config.UnmoddedTextureWeight, "Characters/Farmer/shirts")).ToArray();
            }

            if (name.IsEquivalentTo("Mods/FatteningFarmerFramework/Data/PantsSizes")) {
                this._pantsSizes = Game1.content
                    .Load<Dictionary<int, Size>>("Mods/FatteningFarmerFramework/Data/PantsSizes").Values.ToArray();
                this._pantsSizes = this._pantsSizes
                    .Append(new Size(this._config.UnmoddedTextureWeight, "Characters/Farmer/pants")).ToArray();
            }
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e) {
        if (e.NewLocation.currentEvent != null) {
            this._atFestival = e.NewLocation.currentEvent;
        }
        
        if (this._atFestival != null && e.NewLocation.IsFarm) { //The player just left a festival and went home
            string festival = this._atFestival.FestivalName.ToLower();
            string[] foodFestivals = [
                "egg festival", "flower dance", "luau", "sprit's eve", "feast of the winter star"
            ];

            if (foodFestivals.Contains(festival)) {
                ModData playerStats =
                    this.Helper.Data.ReadSaveData<ModData>("player-stats") ?? new ModData(this._config);
                playerStats.Weight += this._config.FestivalWeightGain;
                this.Helper.Data.WriteSaveData<ModData>("player-stats", playerStats);

                if (this._config.WeightNotifications) {
                    Game1.addHUDMessage(new HUDMessage(
                        $"You had plenty to eat at the festival and now weigh {Math.Round(playerStats.Weight, 0)} {this._config.WeightUnits}."));
                }
                this.Monitor.Log($"{e.Player.Name} ate at the festival, gained {this._config.FestivalWeightGain} {this._config.WeightUnits}, and now weighs {playerStats.Weight} {this._config.WeightUnits}.", LogLevel.Trace);

                this.CalculateSize(e.Player);
            }

            this._atFestival = null;
        }
    }
}