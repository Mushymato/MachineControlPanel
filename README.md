# Machine Control Panel

Do you have many artisan machine related mods with overlapping machine rules? Or a number of different machines in the same automate group that is sending items into the wrong machine? This mod adds way to control which machine rules are active and what items are allowed in the machine.

## Interface

To open the machine control panel, press Q (rebindable) when close to a machine.

You can also open a machine selection menu with LeftCtrl+Q (rebindable).

## Rules

This page displays machine rules on a particular artisan machine, ordered from top to bottom, left to right.

A machine rule is disabled by unchecking the box, and then pressing the save button. Rules are applied before the machine starts processing, disabling a rule or input will not interrupt any ongoing processes.

Some rules take a number of different items, based on conditions or context tags. These are displayed as semi-transparent item with a "note" icon. The tooltip provides more info about their conditions.

Some rules have hard to determine output items, in those cases a question mark icon is displayed instead. Question mark icon is also the placeholder for when a rule has no input item.

Additional items required for a machine (i.e. fuel) are displayed with a "bolt" icon.

### What are Machine Rules?

Artisan machines, as of 1.6, operate on a priority list of rules dictating what items are allowed to be placed into a machine. The rule that accepted the input item then determines what produce comes out. The order of rules (i.e. their priority) is important and the reason why Hops become Pale Ale in a keg, instead of Hops Juice.

Thus, turning off a rule with this mod does not completely prevent the machine from accepting an input. Instead we skip over the disabled rule and check further entries down the list, possibly allowing a different product like the normally impossble Hops Juice to be produced.

Some rules do not accept an item, and instead activates upon other conditions. This allows you to disable machines that generate items on a new day (such as the Coffee Maker) but not other special conditions like machine placed.

## Inputs

This page displays all items that can be put into a particular artisan machine.
An input item is disabled by clicking on the item such that it becomes blacked out, and then pressing the save button.

### Interaction between Rules and Inputs

Should the current rules settings completely prevent an input from being placed into a machine, it will be shown as semi-transparent on the input page and become unclickable until the relevant rules are enabled again.

For example, if the first bone items rule on the bone mill is disabled, all bone items except for bone fragments will become semi-transparent on the input page.

## Compatibility 

* Machines added via [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) is supported by this mod.
    * C# mods that add machines via content pipeline is also supported.
* Extra fuel from [Extra Machine Configs](https://www.nexusmods.com/stardewvalley/mods/22256) is shown on the rules page.
* Both menus in this mod supports [Lookup Anything](https://www.nexusmods.com/stardewvalley/mods/541).
* NOT COMPATIBLE with [Producer Framework Mod](https://www.nexusmods.com/stardewvalley/mods/4970) machines, no supoort is planned.

### Multiplayer

Only the host player is allowed to change machine rules, but everyone can open the control panel to view current settings.

## Configuration

* `Control Panel Key`: Press this key when within range of a machine to open the machine control panel (Default: Q).
* `Machine Select Key`: Press this key to open a selection menu for all machines in the game (Default: LeftControl+Q).
* `Save on Change`: Automatically save changes when closing the control panel or when changing pages.
* `Default Page`: Page of control panel to display by default.
* `Open Machine Select Menu`: Once a save is loaded, this option will display a button that opens the machine select menu, useful in case you are out of keybinds.

### Save Data

Machine settings are recorded per farm in the save data, like vanilla save data this is written to the save file at end of the day and lost if you exit the game midday.

In the machine selections menu, an opaque background indicates that machine has settings.

## Translations

* English
* Simplified Chinese

## Special Thanks

* [focustense](https://next.nexusmods.com/profile/focustense/about-me), creator of the excellent [StardewUI](https://github.com/focustense/StardewUI) library used for this mod's UI elements.

### Content Mods used in Screenshots

* Cornucopia
    * [Artisan Machines](https://www.nexusmods.com/stardewvalley/mods/24842)
    * [More Flowers](https://www.nexusmods.com/stardewvalley/mods/20290)
    * [More Crops](https://www.nexusmods.com/stardewvalley/mods/19508)

* Wildflour's Atelier Goods
    * [Forager](https://www.nexusmods.com/stardewvalley/mods/22728)
    * [Gourmand](https://www.nexusmods.com/stardewvalley/mods/22730)
    * [Barista (Cold)](https://www.nexusmods.com/stardewvalley/mods/22731)
    * [Barista (Hot)](https://www.nexusmods.com/stardewvalley/mods/22732)
