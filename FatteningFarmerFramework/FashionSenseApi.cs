using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using static StardewValley.FarmerSprite;

namespace FatteningFarmerFramework
{
    public interface IFashionSenseIApi
    {
        public enum Type
        {
            Unknown,
            Hair,
            Accessory,
            [Obsolete("No longer maintained. Use Accessory instead.")]
            AccessorySecondary,
            [Obsolete("No longer maintained. Use Accessory instead.")]
            AccessoryTertiary,
            Hat,
            Shirt,
            Pants,
            Sleeves,
            Shoes,
            Player
        }

        KeyValuePair<bool, string> SetAppearance(Type appearanceType, string targetPackId, string targetAppearanceName, IManifest callerManifest);
    }
}
