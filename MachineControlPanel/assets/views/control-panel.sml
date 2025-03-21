<lane orientation="vertical" horizontal-content-alignment="end">
  <lane orientation="horizontal" padding="0,46,26,-16" vertical-content-alignment="end">
    <frame background={@mushymato.MachineControlPanel/sprites/cursors:insetBg} layout="60px 60px">
      <checkbox is-checked={<>ToggleAll} margin="12"/>
    </frame>
    <include name="mushymato.MachineControlPanel/views/includes/global-toggle" *context={:GlobalToggle}/>
    <textinput text={<>SearchText} placeholder={#rule-list.search} background={@mushymato.MachineControlPanel/sprites/cursors:insetBg} layout="240px 60px" text-color="#43111B" focusable="true"/>
  </lane>
  <frame layout="1244px 90%[580..]"
    background={@Mods/StardewUI/Sprites/MenuBackground}
    border={@Mods/StardewUI/Sprites/MenuBorder}
    border-thickness="32, 36, 24, 36"
    *switch={PageIndex} horizontal-content-alignment="middle">
    <lane *float="above" orientation="horizontal" vertical-content-alignment="end" margin="36,0,0,-24">
      <tab-label page="1" text={#rule-list.rules} margin={TabMarginRules} />
      <tab-label *if={HasInputs} page="2" text={#rule-list.inputs} margin={TabMarginInputs}/>
      <image sprite={:MachineData}
        tooltip={:MachineTooltip}
        layout="48px 96px"
        margin="12,-16,12,0"
        fit="Contain"
        horizontal-alignment="middle"
        vertical-alignment="middle"
      />
      <label text={:MachineName} tooltip={:MachineTooltip} font="dialogue" color="white" margin="0,24" />
    </lane>
    <!-- Rules -->
    <scrollable *case="1" peeking="128" scrollbar-margin="8,0,0,0">
      <grid item-layout="length: 88" padding="4,4" horizontal-item-alignment="middle">
        <lane *repeat={RuleEntriesFiltered}
          pointer-enter=|~ControlPanelContext.HandleHoverRuleEntry(RIE)|
          pointer-leave=|~ControlPanelContext.HandleHoverRuleEntry()|
          opacity={Opacity}
          orientation="vertical" margin="8"
          horizontal-content-alignment="middle">
          <checkbox *if={Active} is-checked={<>State} margin="0,8,0,12"/>
          <spacer *!if={Active} layout="36px 56px" />
          <rule-icon *repeat={:Inputs} />
          <spacer layout={:InputSpacerLayout}/>
          <image sprite={SpinningCaret}
            layout="36px 36px"
            margin="18,12,18,12"
            +hover:scale="1.1"
            +transition:scale="100ms EaseInSine"/>
          <rule-icon *repeat={:Outputs} />
        </lane>
      </grid>
    </scrollable>
    <!-- Inputs -->
    <scrollable *case="2" peeking="128" scrollbar-margin="8,0,0,0">
      <lane orientation="vertical">
        <lane orientation="horizontal" margin="16,8">
          <image *repeat={QualityStars} sprite={:Sprite} tint={Tint}
            layout="24px 24px"
            margin="4"
            focusable="true"
            left-click=|ToggleState()|
            +hover:scale="1.1"
            +transition:scale="100ms EaseInSine"/>
        </lane>
        <image sprite={@Mods/StardewUI/Sprites/ThinHorizontalDivider} layout="stretch content" margin="0,0,8,0" fit="Stretch"/>
        <grid *case="2" item-layout="length: 76+" horizontal-item-alignment="middle">
          <panel *repeat={InputItemsFiltered} tooltip={:Tooltip} left-click=|ToggleState()| focusable="true">
            <image *repeat={:SpriteLayers} sprite={:Sprite} tint={^Tint} padding={:Padding} layout={:Layout}
              margin="6"
              focusable="true"
              +hover:scale="1.1"
              +transition:scale="100ms EaseInSine"/>
          </panel>
        </grid>
      </lane>
    </scrollable>
</frame>
</lane>

<template name="tab-label">
  <frame
    background={@mushymato.MachineControlPanel/sprites/cursors:tabBg}
    padding="18,16,18,12"
    focusable="true"
    margin={&margin}
    left-click=|ChangePage(&page)|
    >
    <label text={&text}/>
  </frame>
</template>

<template name="rule-icon">
  <panel tooltip={:Tooltip} left-click=|ShowSubItemGrid()| focusable="true" padding="4" >
    <image *repeat={:SpriteLayers} sprite={:Sprite} tint={:Tint} padding={:Padding} layout={:Layout} margin="2" +state:enabled={:^IsMulti} +state:enabled:opacity="0.6"/>
    <image *if={:IsMulti} sprite={@mushymato.MachineControlPanel/sprites/emojis:note} layout="27px 27px" />
    <panel layout="stretch stretch" horizontal-content-alignment="end" vertical-content-alignment="start">
      <image *if={:IsFuel} sprite={@mushymato.MachineControlPanel/sprites/emojis:bolt} layout="27px 27px"/>
      <image *repeat={:EMCByProductOneItem} sprite={:Sprite} tint={:Tint} padding={:Padding} layout={:Layout}/>
    </panel>
    <panel *if={:HasQualityStar} layout="stretch stretch" horizontal-content-alignment="start" vertical-content-alignment="end">
      <image sprite={:QualityStar} layout="24px 24px"/>
    </panel>
    <panel *if={:ShowCount} layout="stretch stretch"  horizontal-content-alignment="end" vertical-content-alignment="end">
      <digits number={:Count} scale="3"/>
    </panel>
  </panel>
</template>