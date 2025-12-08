# Machine Control Panel

Do you have many artisan machine related mods with overlapping machine rules? Or a number of different machines in the same automate group that is sending items into the wrong machine? This mod adds way to control which machine rules are active and what items are allowed in the machine.

## Interface

To open the machine control panel, press Q (rebindable) when close to a machine.

You can also open a machine selection menu with LeftCtrl+Q (rebindable).

## Rules

This page displays machine rules on a particular artisan machine, ordered from top to bottom, left to right.

A machine rule is disabled by unchecking the box, and then pressing the save button. Rules are applied before the machine starts processing, disabling a rule or input will not interrupt any ongoing processes.

Additional items required for a machine (i.e. fuel) are displayed with a "bolt" icon.

#### What are Machine Rules?

Artisan machines, as of 1.6, operate on a priority list of rules dictating the outputs of a machine. The most common category of rules are those which trigger when player places an item into the machine. Each item is checked against rule from first to last, and the first matching rule accepts the input item and determine what the output item will be.

Some rules take a number of different items, based on conditions or context tags. These are displayed as semi-transparent item with a "note" icon. The tooltip provides more info about their conditions, and clicking the icon displays all matched items in a grid. Advanced conditions are marked with a exclaimation mark, and displayed in the tooltip. Some rules have hard to determine output items, in those cases a question mark icon is displayed instead. Question mark icon is also the placeholder for rules that don't accept input items.

Besides the item based rules, this mod also let you turn off rules that produce without need for input, such as Coffee Maker. Disabling this type of rule may cause placed machines to not work even after enabling the rule again, until the machine in question is removed and placed again.

Turning off a rule with this mod does not always prevent the machine from accepting an input. If another rule further down the list that is normally unreachable can accept the item, it's possible to get items not obtainable otherwise, such as Hops Juice from a keg instead of Pale Ale. This can be useful in case where multiple mods added rules to the same machine, and you want output from a particular mod. For case where you want to completely prevent a machine from taking a particular item, use the inputs tab instead.

Crab Pots and Tappers seem like machines and get treated as such by automation mods, but they are not supported by this mod. In a similar vein, C# mods that add bespoke machine like logic outside of `Data/Machines` are also not supported.

## Inputs

This page displays all items that can be put into a particular artisan machine.
An input item is disabled by clicking on the item such that it becomes blacked out, and then pressing the save button.
You can also prevent certain quality items from being used with the 4 quality icons at the top, this is general and not per input item.

### Interaction between Rules and Inputs

Should the current rules settings completely prevent an input from being placed into a machine, it will be shown as semi-transparent on the input page and become uncheckable until the relevant rules are enabled again.

For example, if the first bone items rule on the bone mill is disabled, all bone items except for bone fragments will become semi-transparent on the input page.

## Global and Local

Both Inputs and Rules support disabling rules and inputs for the current location only, or for everywhere in the world. You can switch between the two mods by clicking the globe icon.

A rule that is disabled everywhere will be disabled for current location regardless of currenct location settings.

## Search

There is a search box in both the machine select menu and the control panel menu.
- In the machine select menu, this searches by machine name or qualified id.
- In the control panel's rules page, this searches by input/output or description of rule.
- IIn the control panel's inputs page, this searches by input item.

## Compatibility 

* Machines added via [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) is supported by this mod.
    * C# mods that add machines via content pipeline targeting `Data/Machines` are also supported.
* [Furniture Machines](https://www.nexusmods.com/stardewvalley/mods/31678) are supported by this mod.
* [Extra Machine Configs](https://www.nexusmods.com/stardewvalley/mods/22256):
    * Extra fuel and rule specific output is displayed.
    * Output byproducts are displayed with an icon on the top right of the output item. Right-click to show all byproducts.
* Both menus in this mod supports [Lookup Anything](https://www.nexusmods.com/stardewvalley/mods/541).
* You can open the two menus with [Iconic Framework](https://www.nexusmods.com/stardewvalley/mods/11026).
    * Left click opens the machine select menu.
    * Right click opens a control panel, if a valid machine is nearby.
* __NOT COMPATIBLE__ with [Producer Framework Mod](https://www.nexusmods.com/stardewvalley/mods/4970) machines, no support is planned.

### Multiplayer

Only the host player is allowed to change machine rules, but everyone can open the control panel to view current settings.

## Configuration

* `Control Panel Key`: Press this key when within range of a machine to open the machine control panel
    * Default: Q
* `Machine Select Key`: Press this key to open a selection menu for all machines in the game
    * Default: LeftControl+Q
* `Progression Mode`: On the machine select page, hide not yet obtained machines.
* `Default Page`: Page of control panel to display by default.
* `Default Is Global`: Page of control panel to display by default.
* `Config Per Save`: Determines how the save data is retained.
* `Alt Question Mark`: Use a more visible question mark icon for special outputs (previously the default icon).
* `Open Machine Select Menu`: Once a save is loaded, this option will display a button that opens the machine select menu, useful in case you are out of keybindings.

### Save Data

Machine settings are recorded per farm in the save data, like vanilla save data this is written to the save file at end of the day and lost if you exit the game midday.

You can change this by disabling `Config Per Save` in the configurations. With this, the machine settings are saved to global app data immediately on change.

In the machine selections menu, an opaque background indicates that machine has settings.

## Translations

* English
* 简体中文
* Русский (by [ellatuk](https://github.com/ellatuk))


## Special Thanks

* focustense, creator of the excellent [StardewUI](https://github.com/focustense/StardewUI) library used for this mod's UI elements.
