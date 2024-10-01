# Machine Control Panel

Do you have many artisan machine related mods with overlapping machine rules? Or a number of different machines in the same automate group that is sending items into the wrong machine? This mod adds way to control which machine rules are active and what items are allowed in the machine.

## Interface

To open the machine control panel, press Q (rebindable) when close to a machine. You can also open a view of all machines with LeftCtrl+Q (rebindable).

## Rules

This page displays all machine rules on a particular artisan machine, ordered from top to bottom, left to right.
A machine rule is disabled by unchecking the box, and then pressing the save button.
Rules are applied before the machine starts processing, disabling a rule or input will not interrupt any ongoing processes.

### What are Machine Rules?

Artisan machines, as of 1.6, operate on a priority list of rules dictating what items are allowed to be placed into a machine. The rule that accepted the input item then determines what produce comes out. The order of rules (i.e. their priority) is important and the reason why Hops become Pale Ale in a keg, instead of Hops Juice.

Thus, turning off a with this mod rule does not completely prevent the machine from accepting an input. Instead we skip over the disabled rule and check further entries down the list, possibly allowing a different product like the normally impossble Hops Juice to be produced.

Some rules do not accept an item, and instead activates upon other conditions. This allows you to disable machines that generate items on a new day (such as the Coffee Maker) but not other special conditions like machine placed.

## Inputs

This page displays all items that can be put into a particular artisan machine.
An input item is disabled by clicking on the item such that it becomes blacked out, and then pressing the save button.

### Interaction between Rules and Inputs

Should the current rules settings completely prevent an input from being placed into a machine, it will be shown as half transparent on the input page and become unclickable until the relevant rules are enabled again. For example, if you disable the bone items rule on the bone mill, all bone items except for bone fragments will become transparent.

## Configuration

Describe the individual configuration attributes below (and delete this line).

* `Option1`: Explanation of option 1.
* `Option2`: Whether to <do thing that option 2 does>.

## Multiplayer

Currently, only the host player is allowed to change machine rules, but everyone can open the menu to view settings.

## Compatibility 

* Machines added via [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) is supported by this mod. C# machines that also go through content pipeline in similar fashion will also work.
* Extra fuel from [Extra Machine Configs](https://www.nexusmods.com/stardewvalley/mods/22256) is shown on the rules page.
* No support is planned for [Producer Framework Mod](https://www.nexusmods.com/stardewvalley/mods/4970) machines.
* The UI

## See Also

* [Changelog](CHANGELOG.md)
