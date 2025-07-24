Thank you for downloading the Fattening Farmer Framework!

The Fattening Farmer Framework includes all the program logic required to calculate the farmer's weight and change body and clothing sprites based on weight. No size assets are included with the base mod. Instead, the FFF provides an interface for content packs to add their assets at any weight the artist chooses. The only hard part of making a content pack for the Fattening Farmer Framework is drawing the textures. Body, shirt, and pants textures can be added, and body textures are sorted by gender for increased compatibility.

Required dependencies:
Stardew Modding API (SMAPI)
Content Patcher

Suggested dependencies:
Generic Mod Config Menu

For mod users:
Make sure SMAPI and Content Patcher are installed, download "Fattening Farmer Framework.zip", and extract it into your mods folder, wherever Stardew Valley is installed. Then add whatever content packs you like.

For mod authors:
Content packs for the Fattening Farmer Framework use Content Patcher to add assets to Stardew Valley and to add information about their assets to the FFF's data files.
If you've never made a content pack before, check out this guide on making mods with Content Patcher before you read further here.

Make sure your manifest.json file has the following lines:

    "ContentPackFor": {
    "UniqueID": "Pathoschild.ContentPatcher"
    },
    "Dependencies" : [
    {
    "UniqueID": "GoddessVirtue.FatteningFarmerFramework"
    }
    ]

Use the Load and EditData actions in your content.json, like so:
{
"Format": "2.5.0",
    "Changes": [
        {
            "Action": "Load",
            "LogName": "Add a texture to content pipeline",
            "Target": "{{ModID}}/texture_asset_name",
            "FromFile": "assets/texture_file_name.png"
        },
        {
            "Action": "EditData",
            "LogName": "Add data on a texture to female body sizes data for Fattening Farmer Framework",
            "Target": "Mods/FatteningFarmerFramework/Data/FemaleBodySizes,
            "Entries": {
                "400": {
                    "Weight": "400",
                    "ContentPackID": "{{ModID}}",
                    "Texture": "{{ModID}}/texture_asset_name"
                }
            }
        }
    ]
}
This example file adds the texture in your content pack folder at "assets/texture_file_name.png" to the list of female body sizes, at weight 400 (pounds, we'll assume for now). So when the player weighs 400 pounds, their body texture will be replaced with texture_file_name.png.

"{{ModID}}/texture_asset_name" is the name for your texture available to the game's files and to the Fattening Farmer Framework. It can be anything, but you probably want it to be unique so it isn't overwritten. That's why I suggest including {{ModID}}, which Content Patcher will automatically replace with your mod's unique ID.

The player weight the texture is for, in this case 400, is repeated twice to improve compatibility. If another content pack also tries to add a 400lb female body, Content Patcher will pick one for the Fattening Farmer Framework to use, and the other won't be loaded. You can use Content Patcher's optional "Priority" field if you're worried about users installing another content pack that replaces yours. If a user installs two different 400lb female bodies, the Fattening Farmer Framework has no way to tell which one the user actually wants to see, so it treats the conflict as a problem for the user to deal with. I recommend you take the same approach.

Have fun! 